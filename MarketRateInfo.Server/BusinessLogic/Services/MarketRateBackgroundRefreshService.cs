using Akka.Streams.Dsl;
using MarketRateInfo.Server.Akka;
using MarketRateInfo.Server.BusinessLogic.ExternalRates.Models;
using MarketRateInfo.Server.BusinessLogic.Interfaces;
using MarketRateInfo.Server.BusinessLogic.Models;
using MarketRateInfo.Server.DataAccess.Repositories;
using System.Collections.Immutable;

namespace MarketRateInfo.Server.BusinessLogic.Services;

/// <summary>
/// Executes a background service that periodically refreshes market rates for configured currency pairs using Akka.NET
/// streams and dependency injection.
/// </summary>
/// <remarks>Refreshes are performed every 30 minutes. Handles cancellation and logs detailed information about
/// the refresh process, including counts of refreshed, skipped, and failed pairs.</remarks>
public sealed class MarketRateBackgroundRefreshService : BackgroundService
{
	private readonly IServiceProvider _serviceProvider;

	private readonly AkkaRuntime _akkaRuntime;

	private readonly ILogger<MarketRateBackgroundRefreshService> _logger;

	private readonly TimeSpan _period = TimeSpan.FromMinutes(30L);

	/// <summary>
	/// Initializes a new instance of the MarketRateBackgroundRefreshService class.
	/// </summary>
	/// <param name="serviceProvider">The service provider for dependency resolution.</param>
	/// <param name="akkaRuntime">The Akka.NET runtime instance.</param>
	/// <param name="logger">The logger used for logging operations.</param>
	public MarketRateBackgroundRefreshService(IServiceProvider serviceProvider, AkkaRuntime akkaRuntime, ILogger<MarketRateBackgroundRefreshService> logger)
	{
		this._serviceProvider = serviceProvider;
		this._akkaRuntime = akkaRuntime;
		this._logger = logger;
	}

	/// <summary>
	/// Executes the background service, periodically refreshing rates until cancellation is requested.
	/// </summary>
	/// <param name="stoppingToken">Token used to signal cancellation of the operation.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using PeriodicTimer timer = new PeriodicTimer(_period);
		await RefreshRatesSafelyAsync(stoppingToken);
		while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
		{
			await RefreshRatesSafelyAsync(stoppingToken);
		}
	}

	/// <summary>
	/// Refreshes market rates for all configured currency pairs in a safe, asynchronous manner.
	/// </summary>
	/// <param name="stoppingToken">Token to monitor for cancellation requests.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	private async Task RefreshRatesSafelyAsync(CancellationToken stoppingToken)
	{
		try
		{
			using IServiceScope scope = _serviceProvider.CreateScope();
			IMarketRateRepository repository = scope.ServiceProvider.GetRequiredService<IMarketRateRepository>();
			IReadOnlyList<string> symbols = await repository.GetSymbolsAsync(stoppingToken);
			if (symbols.Count == 0)
			{
				this._logger.LogInformation("Background market-rate refresh skipped: no currency pairs are configured.");
				return;
			}
			DateTime today = DateTime.UtcNow.Date;
			this._logger.LogInformation("Starting background market-rate refresh for {PairCount} currency pairs up to {CurrentDate:yyyy-MM-dd}.", symbols.Count, today);
			IImmutableList<MarketRatePairRefreshResult> results = await Source.From(symbols).SelectAsync(Math.Max(1, symbols.Count), (string symbol) => RefreshPairAsync(symbol, today, stoppingToken)).RunWith(Sink.Seq<MarketRatePairRefreshResult>(), _akkaRuntime.Materializer);
			int refreshedCount = results.Count((MarketRatePairRefreshResult x) => x.RatesSaved > 0);
			int skippedCount = results.Count((MarketRatePairRefreshResult x) => x.Skipped);
			int failedCount = results.Count((MarketRatePairRefreshResult x) => x.Failed);
			int savedCount = results.Sum((MarketRatePairRefreshResult x) => x.RatesSaved);
			this._logger.LogInformation("Background market-rate refresh completed. Pairs: {PairCount}, refreshed: {RefreshedCount}, skipped: {SkippedCount}, failed: {FailedCount}, rates saved: {RatesSaved}.", symbols.Count, refreshedCount, skippedCount, failedCount, savedCount);
		}
		catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
		{
			this._logger.LogInformation("Background market-rate refresh was cancelled.");
		}
		catch (Exception ex2)
		{
			Exception ex3 = ex2;
			this._logger.LogError(ex3, "Error during background market-rate refresh.");
		}
	}

	/// <summary>
	///Refreshes market rates for the specified symbol up to the given date, initializing if no rates exist.
	/// </summary>
	/// <param name="symbol">The market symbol to refresh.</param>
	/// <param name="today">The target date for refreshing rates.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the refresh result for the market rate
	/// pair.</returns>
	private async Task<MarketRatePairRefreshResult> RefreshPairAsync(string symbol, DateTime today, CancellationToken cancellationToken)
	{
		try
		{
			using IServiceScope scope = _serviceProvider.CreateScope();
			IMarketRateRepository repository = scope.ServiceProvider.GetRequiredService<IMarketRateRepository>();
			IMarketRateRefreshService refreshService = scope.ServiceProvider.GetRequiredService<IMarketRateRefreshService>();
			DateTime? lastRateDate = await repository.GetLatestRateDateAsync(symbol, cancellationToken);
			if (!lastRateDate.HasValue)
			{
				this._logger.LogInformation("Pair {Symbol} has no stored rates. Initializing it with {Date:yyyy-MM-dd}.", symbol, today);
				MarketRateRefreshResult initialResult = await refreshService.RefreshAsync(symbol, today, today, cancellationToken);
				return new MarketRatePairRefreshResult(symbol, initialResult.RatesSaved, initialResult.RatesSaved == 0, Failed: false);
			}
			DateTime startDate = lastRateDate.Value.AddDays(1.0);
			if (startDate > today)
			{
				this._logger.LogDebug("Pair {Symbol} is up to date. Last rate date: {LastRateDate:yyyy-MM-dd}.", symbol, lastRateDate.Value);
				return new MarketRatePairRefreshResult(symbol, 0, Skipped: true, Failed: false);
			}
			this.	_logger.LogInformation("Refreshing pair {Symbol} from {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}. Last stored rate: {LastRateDate:yyyy-MM-dd}.", symbol, startDate, today, lastRateDate.Value);
			MarketRateRefreshResult result = await refreshService.RefreshAsync(symbol, startDate, today, cancellationToken);
			return new MarketRatePairRefreshResult(symbol, result.RatesSaved, result.RatesSaved == 0, Failed: false);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex2)
		{
			Exception ex3 = ex2;
			this._logger.LogError(ex3, "Failed to refresh market rates for pair {Symbol}.", symbol);
			return new MarketRatePairRefreshResult(symbol, 0, Skipped: false, Failed: true);
		}
	}
}
