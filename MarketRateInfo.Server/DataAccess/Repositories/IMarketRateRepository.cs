using MarketRateInfo.Server.DataAccess.Entities;

namespace MarketRateInfo.Server.DataAccess.Repositories;

/// <summary>
/// Provides access to market rate data and currency pair configuration.
/// </summary>
public interface IMarketRateRepository
{
    /// <summary>
    /// Gets all market rates stored in the database.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A collection of all stored market rates.</returns>
    Task<IReadOnlyList<MarketRate>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent market rate for the specified currency pair.
    /// </summary>
    /// <param name="symbol">Currency pair symbol, for example <c>EUR/USD</c> or <c>BTC/USD</c> or <c>ETH/USD</c>.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The most recent market rate for the specified symbol, or <see langword="null"/> if no rate exists.</returns>
    Task<MarketRate?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configured currency pair symbols.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A collection of configured currency pair symbols.</returns>
    Task<IReadOnlyList<string>> GetSymbolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the date of the most recent market rate for the specified currency pair.
    /// </summary>
    /// <param name="symbol">Currency pair symbol, for example <c>EUR/USD</c> or <c>BTC/USD</c> or <c>ETH/USD</c>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The date of the most recent stored market rate, or <see langword="null"/> if no rate exists.</returns>
    Task<DateTime?> GetLatestRateDateAsync(
        string symbol,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets historical market rates for the specified currency pair within the specified date range.
    /// </summary>
    /// <param name="symbol">Currency pair symbol, for example <c>EUR/USD</c> or <c>BTC/USD</c> or <c>ETH/USD</c>.</param>
    /// <param name="fromUtc">Start of the date range, inclusive.</param>
    /// <param name="toUtcExclusive">End of the date range, exclusive.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Market rates found within the specified date range.</returns>
    Task<IReadOnlyList<MarketRate>> GetHistoryAsync(
        string symbol,
        DateTime fromUtc,
        DateTime toUtcExclusive,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the dates for which no market rate exists for the specified currency pair.
    /// </summary>
    /// <param name="symbol">Currency pair symbol, for example <c>EUR/USD</c> or <c>BTC/USD</c> or <c>ETH/USD</c>.</param>
    /// <param name="startDate">The start date of the range, inclusive.</param>
    /// <param name="endDate">The end date of the range, inclusive.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of dates for which no market rate exists.</returns>
    Task<IReadOnlyList<DateTime>> GetMissingDatesAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts new market rates or updates existing rates for the same currency pair and date.
    /// </summary>
    /// <param name="rates">Market rates to insert or update.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The number of records that were inserted or updated.</returns>
    Task<int> UpsertRatesAsync(IReadOnlyCollection<MarketRate> rates, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a currency pair to the list of configured pairs.
    /// </summary>
    /// <param name="baseCurrency">Base currency code, for example <c>EUR</c> or <c>BTC</c>.</param>
    /// <param name="quoteCurrency">Quote currency code, for example <c>USD</c> or <c>UAH</c>.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns><see langword="true"/> if the currency pair was added; otherwise, <see langword="false"/> if it already exists.</returns>
    Task<bool> AddCurrencyPairAsync(
        string baseCurrency,
        string quoteCurrency,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a currency pair from the list of configured pairs.
    /// </summary>
    /// <param name="baseCurrency">Base currency code, for example <c>EUR</c> or <c>BTC</c>.</param>
    /// <param name="quoteCurrency">Quote currency code, for example <c>USD</c> or <c>UAH</c>.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns><see langword="true"/> if the currency pair was removed; otherwise, <see langword="false"/> if it does not exist.</returns>
    Task<bool> RemoveCurrencyPairAsync(
        string baseCurrency,
        string quoteCurrency,
        CancellationToken cancellationToken = default);
}