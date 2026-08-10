using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using AkkaNetDemo;
using AkkaNetDemo.Interfaces;
using AkkaNetDemo.Models;
using AkkaNetDemo.Services;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;

class Program
{
    private const int MaxTotalErrors = 4;
    private const int MaxRetriesPerRequest = 4;
    private const int DaysBack = 30;
    private const int QueueCapacity = 100;
    private const string StandardDateFormat = "yyyy-MM-dd";
    private const string UserAgentHeader = "AkkaNetDemo/1.0";
    private static readonly TimeSpan RuntimeMetricsInterval = TimeSpan.FromSeconds(5);

    static async Task Main(string[] args)
    {
        // ---------------------------------------------------------
        // Serilog
        // ---------------------------------------------------------

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                formatter: new CompactJsonFormatter(),
                path: "logs/akka-demo-.json",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true)
            .CreateLogger();

        Log.Information("Starting BTC stream demo");

        // ---------------------------------------------------------
        // Runtime metrics
        // ---------------------------------------------------------

        var runtimeStore = new RuntimeMetricsStore();

        // ---------------------------------------------------------
        // HTTP endpoint for Grafana Infinity
        // http://localhost:5000/api/runtime
        // ---------------------------------------------------------

        using var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:5000/");
        listener.Start();

        Log.Information("Runtime metrics API started at http://localhost:5000/api/runtime");

        var httpServerTask = Task.Run(async () =>
        {
            while (listener.IsListening)
            {
                try
                {
                    var context = await listener.GetContextAsync();

                    try
                    {
                        if (context.Request.Url?.AbsolutePath == "/api/runtime")
                        {
                            var json = JsonSerializer.Serialize(runtimeStore.GetAll());

                            context.Response.StatusCode = (int)HttpStatusCode.OK;
                            context.Response.ContentType = "application/json";
                            context.Response.ContentEncoding = Encoding.UTF8;

                            await using var writer = new StreamWriter(
                                context.Response.OutputStream,
                                Encoding.UTF8);

                            await writer.WriteAsync(json);
                        }
                        else
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        }
                    }
                    finally
                    {
                        context.Response.Close();
                    }
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        });

        // ---------------------------------------------------------
        // OpenTelemetry Metrics
        // Runtime data is exposed through /api/runtime.
        // ---------------------------------------------------------

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(Telemetry.ServiceName))
            .AddMeter(Telemetry.ServiceName)
            .AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation()
            .Build();

        // ---------------------------------------------------------
        // OpenTelemetry Traces
        // ---------------------------------------------------------

        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(Telemetry.ServiceName))
            .AddSource(Telemetry.ServiceName)
            .AddHttpClientInstrumentation()
            .Build();

        try
        {
            // ---------------------------------------------------------
            // Actor system
            // ---------------------------------------------------------

            using var system = ActorSystem.Create("MySystem");

            // ---------------------------------------------------------
            // Stream materializer
            // ---------------------------------------------------------

            using var materializer = system.Materializer();

            // ---------------------------------------------------------
            // HTTP client
            // ---------------------------------------------------------

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgentHeader);

            // ---------------------------------------------------------
            // Services
            // ---------------------------------------------------------

            IBitcoinRateService bitcoinRateService = new BitcoinRateService(httpClient);

            // Adaptive throttle:
            // 0 -> 10 -> 20 -> 40 -> 60 seconds.
            // Success resets the throttle back to 0.
            var adaptiveThrottle = new AdaptiveThrottle();

            // ---------------------------------------------------------
            // Error state
            // ---------------------------------------------------------

            int consecutiveHttpErrorCount = 0;
            DateTime? degradedSince = null;

            // ---------------------------------------------------------
            // Runtime metrics collector
            //
            // IMPORTANT:
            // adaptiveThrottle must be created before this task.
            // ---------------------------------------------------------

            using var timer = new PeriodicTimer(RuntimeMetricsInterval);

            var metricsTask = Task.Run(async () =>
            {
                try
                {
                    while (await timer.WaitForNextTickAsync())
                    {
                        runtimeStore.Add(new RuntimeMetricPoint
                        {
                            Time = DateTime.UtcNow,
                            GcHeapBytes = GC.GetTotalMemory(false),
                            ThreadPoolThreads = ThreadPool.ThreadCount,
                            WorkingSetBytes = Environment.WorkingSet,

                            // Current delay configured by the adaptive
                            // throttle: 0 / 10000 / 20000 / 40000 / 60000 ms.
                            ThrottleDelayMs = adaptiveThrottle.CurrentDelay.TotalMilliseconds
                        });
                    }
                }
                catch (OperationCanceledException)
                {
                    // Normal shutdown.
                }
            });

            // ---------------------------------------------------------
            // Input dates
            // ---------------------------------------------------------

            var startDate = DateTime.UtcNow.AddDays(-DaysBack);

            var dates = Enumerable.Range(0, DaysBack)
                .Select(offset => startDate.AddDays(offset).ToString(StandardDateFormat))
                .ToList();

            // ---------------------------------------------------------
            // Kill switch
            // ---------------------------------------------------------

            var sharedKillSwitch = KillSwitches.Shared("btc-rate-kill-switch");
            bool stoppedByKillSwitch = false;

            // ---------------------------------------------------------
            // Worker
            // ---------------------------------------------------------

            Func<string, Task<BtcRate>> fetchBtcRateWithAdaptiveThrottle = async date =>
            {
                if (Volatile.Read(ref consecutiveHttpErrorCount) >= MaxTotalErrors)
                    return new BtcRate(date, Constants.FailedPriceSentinel);

                for (var attempt = 1; attempt <= MaxRetriesPerRequest; attempt++)
                {
                    // The current adaptive delay is applied immediately
                    // before the actual HTTP request.
                    await adaptiveThrottle.WaitAsync();

                    Telemetry.Requests.Add(1);

                    if (attempt > 1)
                        Telemetry.Retries.Add(1);

                    using var activity = Telemetry.ActivitySource.StartActivity(
                        "btc.fetch",
                        ActivityKind.Client);

                    activity?.SetTag("btc.date", date);
                    activity?.SetTag("retry.attempt", attempt);
                    activity?.SetTag(
                        "throttle.delay_seconds",
                        adaptiveThrottle.CurrentDelay.TotalSeconds);

                    var stopwatch = Stopwatch.StartNew();

                    try
                    {
                        if (attempt > 1)
                        {
                            Log.Information(
                                "Retry BTC request for {Date}, attempt {Attempt}",
                                date,
                                attempt);
                        }

                        // -------------------------------------------------
                        // Actual BTC HTTP request
                        // -------------------------------------------------

                        var result = await bitcoinRateService.GetRateAsync(date);

                        stopwatch.Stop();

                        activity?.SetStatus(ActivityStatusCode.Ok);
                        activity?.SetTag(
                            "http.duration_ms",
                            stopwatch.Elapsed.TotalMilliseconds);

                        // -------------------------------------------------
                        // SUCCESS
                        //
                        // Error counter:
                        //     N -> 0
                        //
                        // Adaptive throttle:
                        //     60 -> 0
                        //     40 -> 0
                        //     20 -> 0
                        //     10 -> 0
                        //
                        // IMPORTANT:
                        // The reset happens AFTER the successful request.
                        // -------------------------------------------------

                        var previousErrorCount = Interlocked.Exchange(
                            ref consecutiveHttpErrorCount,
                            0);

                        var previousThrottleDelay =
                            adaptiveThrottle.CurrentDelay;

                        adaptiveThrottle.OnSuccess();

                        if (previousErrorCount > 0)
                        {
                            Log.Information(
                                "Request succeeded. Error counter reset to 0. Adaptive throttle reset from {PreviousDelaySeconds}s to {CurrentDelaySeconds}s.",
                                previousThrottleDelay.TotalSeconds,
                                adaptiveThrottle.CurrentDelay.TotalSeconds);
                        }

                        // -------------------------------------------------
                        // Downstream recovery
                        // -------------------------------------------------

                        if (degradedSince.HasValue)
                        {
                            var duration = DateTime.UtcNow - degradedSince.Value;

                            Log.Information(
                                "Downstream recovered after {DurationSeconds}s",
                                duration.TotalSeconds);

                            degradedSince = null;
                        }

                        return result;
                    }
                    catch (HttpRequestException ex)
                    {
                        stopwatch.Stop();

                        Telemetry.RequestDuration.Record(
                            stopwatch.Elapsed.TotalSeconds);

                        Telemetry.Failures.Add(1);

                        activity?.SetStatus(
                            ActivityStatusCode.Error,
                            ex.Message);

                        activity?.SetTag(
                            "http.duration_ms",
                            stopwatch.Elapsed.TotalMilliseconds);

                        degradedSince ??= DateTime.UtcNow;

                        // -------------------------------------------------
                        // FAILED REQUEST
                        //
                        // Increase error counter and throttle AFTER
                        // the failed HTTP request.
                        //
                        // Failure #1:
                        //     0  -> 10
                        //
                        // Failure #2:
                        //     10 -> 20
                        //
                        // Failure #3:
                        //     20 -> 40
                        //
                        // Failure #4:
                        //     40 -> 60
                        // -------------------------------------------------

                        var currentErrors = Interlocked.Increment(
                            ref consecutiveHttpErrorCount);

                        adaptiveThrottle.OnFailure();

                        Log.Warning(
                            "BTC request failed for {Date}. ConsecutiveErrors={Count}. Throttle={ThrottleSeconds}s. Error={Error}",
                            date,
                            currentErrors,
                            adaptiveThrottle.CurrentDelay.TotalSeconds,
                            ex.Message);

                        if (currentErrors >= MaxTotalErrors)
                        {
                            Log.Warning(
                                "Error limit reached. Shutting down stream...");

                            stoppedByKillSwitch = true;
                            sharedKillSwitch.Shutdown();

                            return new BtcRate(
                                date,
                                Constants.FailedPriceSentinel);
                        }

                        continue;
                    }
                    catch (Exception ex)
                    {
                        stopwatch.Stop();

                        Telemetry.RequestDuration.Record(
                            stopwatch.Elapsed.TotalSeconds);

                        activity?.SetStatus(
                            ActivityStatusCode.Error,
                            ex.Message);

                        activity?.SetTag(
                            "http.duration_ms",
                            stopwatch.Elapsed.TotalMilliseconds);

                        Log.Error(
                            ex,
                            "Unexpected error for {Date}",
                            date);

                        return new BtcRate(
                            date,
                            Constants.FailedPriceSentinel);
                    }
                }

                return new BtcRate(
                    date,
                    Constants.FailedPriceSentinel);
            };

            // ---------------------------------------------------------
            // Sink
            // ---------------------------------------------------------

            var sink = Sink.ForEach<BtcRate>(x =>
            {
                if (x.Price != Constants.FailedPriceSentinel)
                {
                    Log.Information(
                        "BTC price for {Date} is {Price}",
                        x.Date,
                        x.Price);
                }
            });

            // ---------------------------------------------------------
            // Stream pipeline
            // ---------------------------------------------------------

            var (queue, completionTask) = Source.Queue<string>(
                    QueueCapacity,
                    OverflowStrategy.Backpressure)
                .Throttle(
                    elements: 1,
                    per: TimeSpan.FromSeconds(1),
                    maximumBurst: 1,
                    mode: ThrottleMode.Shaping)
                .SelectAsync(
                    1,
                    fetchBtcRateWithAdaptiveThrottle)
                .Via(sharedKillSwitch.Flow<BtcRate>())
                .ToMaterialized(
                    sink,
                    Keep.Both)
                .Run(materializer);

            Log.Information(
                "Queue type: {QueueType}",
                queue.GetType().FullName);

            Log.Information(
                "Task status at start: {Status}",
                completionTask.Status);

            Log.Information(
                "--------------------------------------------------");

            foreach (var date in dates)
                await queue.OfferAsync(date);

            queue.Complete();

            await completionTask;

            Log.Information(
                "--------------------------------------------------");

            Log.Information(
                "Task status at end: {Status}",
                completionTask.Status);

            if (stoppedByKillSwitch)
            {
                Log.Warning(
                    "Processing stopped due to too many consecutive HTTP errors.");
            }
            else
            {
                Log.Information(
                    "Processing finished successfully!");
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(
                ex,
                "Application terminated unexpectedly");
        }
        finally
        {
            listener.Stop();

            try
            {
                await httpServerTask;
            }
            catch
            {
                // Expected during listener shutdown.
            }

            Log.CloseAndFlush();
        }
    }
}