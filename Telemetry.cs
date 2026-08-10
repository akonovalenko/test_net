using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AkkaNetDemo;

public static class Telemetry
{
    public const string ServiceName = "AkkaNetDemo";

    // -------------------------------------------------------------
    // Metrics
    // -------------------------------------------------------------

    public static readonly Meter Meter = new(ServiceName, "1.0.0");

    public static readonly Counter<long> Requests =
        Meter.CreateCounter<long>(
            "btc_requests_total",
            description: "Total BTC requests");

    public static readonly Counter<long> Retries =
        Meter.CreateCounter<long>(
            "btc_retries_total",
            description: "Total retry attempts");

    public static readonly Counter<long> Failures =
        Meter.CreateCounter<long>(
            "btc_failures_total",
            description: "Total failed requests");

    public static readonly Histogram<double> RequestDuration =
        Meter.CreateHistogram<double>(
            "btc_request_duration_seconds",
            unit: "s",
            description: "HTTP request duration in seconds");

    // -------------------------------------------------------------
    // Traces
    // -------------------------------------------------------------

    public static readonly ActivitySource ActivitySource =
        new(ServiceName);
}