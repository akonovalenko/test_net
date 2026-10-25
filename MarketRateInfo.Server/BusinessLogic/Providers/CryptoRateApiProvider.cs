using MarketRateInfo.Server.BusinessLogic.ExternalRates.Models;
using MarketRateInfo.Server.BusinessLogic.Interfaces;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

/// <summary>
/// Provides cryptocurrency-to-USD market rates using the CoinGecko API with built-in retry, throttling, and rate limit
/// handling.
/// </summary>
/// <remarks>Supports BTC and ETH to USD conversions. Implements IMarketRateProvider for integration with external
/// market rate systems.</remarks>
public sealed class CryptoRateApiProvider : IMarketRateProvider
{
	private static readonly TimeSpan RequestDelay = TimeSpan.FromSeconds(5L);

	private static readonly TimeSpan DefaultRateLimitDelay = TimeSpan.FromSeconds(30L);

	private readonly HttpClient _httpClient;

	private readonly ILogger<CryptoRateApiProvider> _logger;

	private readonly SemaphoreSlim _throttleLock = new SemaphoreSlim(1, 1);

	private DateTimeOffset _lastRequestTime = DateTimeOffset.MinValue;

	/// <summary>
	/// Initializes a new instance of the CryptoRateApiProvider class with the specified HTTP client and logger.
	/// </summary>
	/// <param name="httpClient">The HTTP client used for sending API requests.</param>
	/// <param name="logger">The logger used for logging information and errors.</param>
	public CryptoRateApiProvider(HttpClient httpClient, ILogger<CryptoRateApiProvider> logger)
	{
		this._httpClient = httpClient;
		this._logger = logger;
	}

	/// <summary>
	/// Determines whether conversion between the specified currency and USD is supported.
	/// </summary>
	/// <param name="fromCurrency">The source currency code.</param>
	/// <param name="toCurrency">The target currency code.</param>
	/// <returns>true if the conversion is supported; otherwise, false.</returns>
	public bool Supports(string fromCurrency, string toCurrency)
	{
		return GetCoinId(fromCurrency) != null && string.Equals(toCurrency, "USD", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Asynchronously retrieves external market rates for the specified currency pair and dates.
	/// </summary>
	/// <param name="fromCurrency">The source currency code.</param>
	/// <param name="toCurrency">The target currency code.</param>
	/// <param name="dates">The collection of dates for which to retrieve rates.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A read-only collection of external market rates for the requested dates.</returns>
	public async Task<IReadOnlyCollection<ExternalMarketRate>> GetRatesAsync(string fromCurrency, string toCurrency, IReadOnlyCollection<DateTime> dates, CancellationToken cancellationToken = default(CancellationToken))
	{
		string coinId = GetCoinId(fromCurrency);
		if (coinId == null || !string.Equals(toCurrency, "USD", StringComparison.OrdinalIgnoreCase))
		{
			_logger.LogDebug("Crypto provider does not support {FromCurrency}/{ToCurrency}.", fromCurrency, toCurrency);
			return Array.Empty<ExternalMarketRate>();
		}
		List<DateTime> requestedDates = dates.Select((DateTime x) => x.Date).Distinct().Order()
			.ToList();
		if (requestedDates.Count == 0)
		{
			return Array.Empty<ExternalMarketRate>();
		}
		this._logger.LogInformation("Fetching {FromCurrency}/{ToCurrency} rates for {Count} requested dates.", fromCurrency, toCurrency, requestedDates.Count);
		List<ExternalMarketRate> rates = new List<ExternalMarketRate>();
		DateTime today = DateTime.UtcNow.Date;
		foreach (DateTime date in requestedDates)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (date > today)
			{
				this._logger.LogWarning("Skipping future {FromCurrency}/{ToCurrency} date {Date}.", fromCurrency, toCurrency, date);
			}
			else
			{
				ExternalMarketRate rate = await FetchWithRetryAsync(coinId, fromCurrency, toCurrency, date, cancellationToken);
				if ((object)rate != null)
				{
					rates.Add(rate);
				}
			}
		}
		this._logger.LogInformation("Fetched {Count} {FromCurrency}/{ToCurrency} rates for requested dates.", rates.Count, fromCurrency, toCurrency);
		return rates;
	}

	/// <summary>
	/// Fetches the external market rate for the specified coin and currency pair on a given date, retrying up to three
	/// times on transient failures or rate limits.
	/// </summary>
	/// <param name="coinId">The CoinGecko identifier for the coin.</param>
	/// <param name="fromCurrency">The source currency code.</param>
	/// <param name="toCurrency">The target currency code.</param>
	/// <param name="date">The date for which to fetch the market rate.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>An ExternalMarketRate if successful; otherwise, null.</returns>
	private async Task<ExternalMarketRate?> FetchWithRetryAsync(string coinId, string fromCurrency, string toCurrency, DateTime date, CancellationToken cancellationToken)
	{
		string endpoint = BuildEndpoint(coinId, date);
		if (endpoint == null)
		{
			this._logger.LogDebug("No CoinGecko endpoint available for {FromCurrency}/{ToCurrency} {Date}.", fromCurrency, toCurrency, date.Date);
			return null;
		}
		this._logger.LogDebug("Fetching {FromCurrency}/{ToCurrency} rate for {Date} using endpoint {Endpoint}.", fromCurrency, toCurrency, date.Date, endpoint);
		for (int attempt = 1; attempt <= 3; attempt++)
		{
			try
			{
				await ThrottleAsync(cancellationToken);
				this._logger.LogDebug("Sending CoinGecko request for {FromCurrency}/{ToCurrency} {Date}. Attempt {Attempt}/{MaxAttempts}. Endpoint: {Endpoint}", fromCurrency, toCurrency, date.Date, attempt, 3, endpoint);
				using HttpResponseMessage response = await _httpClient.GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
				this._logger.LogDebug("CoinGecko response for {FromCurrency}/{ToCurrency} {Date}: {StatusCode}, HTTP/{HttpVersion}.", fromCurrency, toCurrency, date.Date, response.StatusCode, response.Version);
				if (response.StatusCode == HttpStatusCode.Forbidden)
				{
					string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
					this._logger.LogError("CoinGecko returned 403 Forbidden for {FromCurrency}/{ToCurrency} {Date}. Response: {Response}", fromCurrency, toCurrency, date.Date, responseBody);
					return null;
				}
				if (response.StatusCode == HttpStatusCode.TooManyRequests)
				{
					if (attempt >= 3)
					{
						this._logger.LogError("CoinGecko rate limit persisted for {FromCurrency}/{ToCurrency} {Date} after {MaxAttempts} attempts.", fromCurrency, toCurrency, date.Date, 3);
						return null;
					}
					TimeSpan delay = GetRateLimitDelay(response, attempt);
					this._logger.LogWarning("CoinGecko rate limit exceeded for {FromCurrency}/{ToCurrency} {Date}. Attempt {Attempt}/{MaxAttempts}. Retrying after {Delay}.", fromCurrency, toCurrency, date.Date, attempt, 3, delay);
					await Task.Delay(delay, cancellationToken);
					continue;
				}
				if (response.StatusCode >= HttpStatusCode.InternalServerError)
				{
					if (attempt >= 3)
					{
						this._logger.LogError("CoinGecko returned {StatusCode} for {FromCurrency}/{ToCurrency} {Date} after {MaxAttempts} attempts.", response.StatusCode, fromCurrency, toCurrency, date.Date, 3);
						return null;
					}
					TimeSpan delay2 = GetRetryDelay(attempt);
					this._logger.LogWarning("CoinGecko returned {StatusCode} for {FromCurrency}/{ToCurrency} {Date}. Attempt {Attempt}/{MaxAttempts}. Retrying after {Delay}.", response.StatusCode, fromCurrency, toCurrency, date.Date, attempt, 3, delay2);
					await Task.Delay(delay2, cancellationToken);
					continue;
				}
				if (!response.IsSuccessStatusCode)
				{
					string responseBody2 = await response.Content.ReadAsStringAsync(cancellationToken);
					this._logger.LogWarning("CoinGecko returned non-success status {StatusCode} for {FromCurrency}/{ToCurrency} {Date}. Response: {Response}", response.StatusCode, fromCurrency, toCurrency, date.Date, responseBody2);
					return null;
				}
				ExternalMarketRate rate = await ParseRateAsync(response, coinId, fromCurrency, toCurrency, date, cancellationToken);
				if ((object)rate != null)
				{
					this._logger.LogInformation("{FromCurrency}/{ToCurrency} rate for {Date}: {Rate}. Endpoint: {Endpoint}", fromCurrency, toCurrency, date.Date, rate.Rate, endpoint);
				}
				return rate;
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				throw;
			}
			catch (HttpRequestException exception)
			{
				if (attempt >= 3)
				{
					this._logger.LogError(exception, "CoinGecko request failed for {FromCurrency}/{ToCurrency} {Date} after {MaxAttempts} attempts.", fromCurrency, toCurrency, date.Date, 3);
					return null;
				}
				TimeSpan delay3 = GetRetryDelay(attempt);
				this._logger.LogWarning(exception, "CoinGecko request failed for {FromCurrency}/{ToCurrency} {Date}. Attempt {Attempt}/{MaxAttempts}. Retrying after {Delay}.", fromCurrency, toCurrency, date.Date, attempt, 3, delay3);
				await Task.Delay(delay3, cancellationToken);
			}
		}
		return null;
	}

	/// <summary>
	/// Gets the coin identifier for the specified currency code.
	/// </summary>
	/// <param name="currency">The currency code to map to a coin identifier.</param>
	/// <returns>The corresponding coin identifier if recognized; otherwise, null.</returns>
	private static string? GetCoinId(string currency)
	{
		string text = currency.ToUpperInvariant();
		if (1 == 0)
		{
		}
		string result;
		if (text == "BTC")
		{
			result = "bitcoin";
		}
		else
		{
			result = ((!(text == "ETH")) ? null : "ethereum");
		}
		if (1 == 0)
		{
		}
		return result;
	}

	/// <summary>
	/// Builds an endpoint URL for retrieving coin price or historical data based on the specified date.
	/// </summary>
	/// <param name="coinId">The unique identifier of the coin.</param>
	/// <param name="date">The date for which to retrieve data.</param>
	/// <returns>The constructed endpoint URL, or null if the date is in the future.</returns>
	private static string? BuildEndpoint(string coinId, DateTime date)
	{
		DateTime today = DateTime.UtcNow.Date;
		if (date.Date < today)
		{
			string dateString = date.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
			return $"coins/{coinId}/history?date={dateString}" + "&localization=false";
		}
		if (date.Date == today)
		{
			return "simple/price?ids=" + coinId + "&vs_currencies=usd";
		}
		return null;
	}

	/// <summary>
	/// Parses the exchange rate from a CoinGecko API HTTP response for the specified currency pair and date.
	/// </summary>
	/// <param name="response">The HTTP response message containing the CoinGecko API data.</param>
	/// <param name="coinId">The CoinGecko identifier for the cryptocurrency.</param>
	/// <param name="fromCurrency">The source currency code.</param>
	/// <param name="toCurrency">The target currency code.</param>
	/// <param name="date">The date for which to retrieve the exchange rate.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the parsed exchange rate or null if not
	/// found.</returns>
	private async Task<ExternalMarketRate?> ParseRateAsync(HttpResponseMessage response, string coinId, string fromCurrency, string toCurrency, DateTime date, CancellationToken cancellationToken)
	{
		ExternalMarketRate result;
		await using (Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken))
		{
			using JsonDocument document = await JsonDocument.ParseAsync(stream, default, cancellationToken);
			JsonElement root = document.RootElement;
			decimal rate;
			if (date.Date == DateTime.UtcNow.Date)
			{
				if (TryGetCurrentRate(root, coinId, out rate))
				{
					goto IL_0269;
				}
				_logger.LogWarning("CoinGecko current-price response does not contain a valid {FromCurrency}/{ToCurrency} rate for {Date}.", fromCurrency, toCurrency, date.Date);
				result = null;
			}
			else
			{
				if (TryGetHistoricalRate(root, out rate))
				{
					goto IL_0269;
				}
				_logger.LogWarning("CoinGecko historical response does not contain a valid {FromCurrency}/{ToCurrency} rate for {Date}.", fromCurrency, toCurrency, date.Date);
				result = null;
			}
			goto end_IL_015b;
			IL_0269:
			result = new ExternalMarketRate(fromCurrency, toCurrency, rate, date.Date);
			end_IL_015b:;
		}
		return result;
	}

	/// <summary>
	/// Attempts to retrieve the current USD rate for the specified coin from the provided JSON element.
	/// </summary>
	/// <param name="root">The JSON element containing coin rate data.</param>
	/// <param name="coinId">The identifier of the coin to retrieve the rate for.</param>
	/// <param name="rate">When this method returns, contains the USD rate for the specified coin if found; otherwise, zero.</param>
	/// <returns>true if the rate was successfully retrieved and is greater than zero; otherwise, false.</returns>
	private static bool TryGetCurrentRate(JsonElement root, string coinId, out decimal rate)
	{
		rate = 0m;
		if (!root.TryGetProperty(coinId, out var coin) || !coin.TryGetProperty("usd", out var usd))
		{
			return false;
		}
		return usd.TryGetDecimal(out rate) && rate > 0m;
	}

	/// <summary>
	/// Attempts to extract the USD historical rate from the specified JSON element.
	/// </summary>
	/// <param name="root">The JSON element containing market data.</param>
	/// <param name="rate">When this method returns, contains the extracted USD rate if successful; otherwise, zero.</param>
	/// <returns>true if the USD rate was successfully extracted and is greater than zero; otherwise, false.</returns>
	private static bool TryGetHistoricalRate(JsonElement root, out decimal rate)
	{
		rate = 0m;
		if (!root.TryGetProperty("market_data", out var marketData) || !marketData.TryGetProperty("current_price", out var currentPrice) || !currentPrice.TryGetProperty("usd", out var usd))
		{
			return false;
		}
		return usd.TryGetDecimal(out rate) && rate > 0m;
	}

	/// <summary>
	/// Enforces a delay between requests to ensure throttling according to the configured request interval.
	/// </summary>
	/// <param name="cancellationToken">Token to observe for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous throttling operation.</returns>
	private async Task ThrottleAsync(CancellationToken cancellationToken)
	{
		await this._throttleLock.WaitAsync(cancellationToken);
		try
		{
			TimeSpan elapsed = DateTimeOffset.UtcNow - this._lastRequestTime;
			if (elapsed < RequestDelay)
			{
				await Task.Delay(RequestDelay - elapsed, cancellationToken);
			}
			this._lastRequestTime = DateTimeOffset.UtcNow;
		}
		finally
		{
			this._throttleLock.Release();
		}
	}

	/// <summary>
	/// Calculates an exponential backoff delay with added random jitter based on the retry attempt number.
	/// </summary>
	/// <param name="attempt">The zero-based retry attempt number.</param>
	/// <returns>A TimeSpan representing the calculated delay before the next retry.</returns>
	private static TimeSpan GetRetryDelay(int attempt)
	{
		double exponentialSeconds = Math.Pow(2.0, attempt);
		int jitterMilliseconds = Random.Shared.Next(0, 1000);
		return TimeSpan.FromSeconds(exponentialSeconds) + TimeSpan.FromMilliseconds(jitterMilliseconds);
	}

	/// <summary>
	/// Calculates the delay required before retrying a request based on the rate limit information in the response headers
	/// and the current attempt number.
	/// </summary>
	/// <param name="response">The HTTP response message containing rate limit headers.</param>
	/// <param name="attempt">The current retry attempt number.</param>
	/// <returns>A TimeSpan representing the delay before the next retry.</returns>
	private static TimeSpan GetRateLimitDelay(HttpResponseMessage response, int attempt)
	{
		RetryConditionHeaderValue retryAfter = response.Headers.RetryAfter;
		if (retryAfter != null)
		{
			TimeSpan? delta = retryAfter.Delta;
			if (delta.HasValue)
			{
				TimeSpan delta2 = delta.GetValueOrDefault();
				if (delta2 > TimeSpan.Zero)
				{
					return delta2;
				}
			}
			DateTimeOffset? date = retryAfter.Date;
			if (date.HasValue)
			{
				DateTimeOffset retryDate = date.GetValueOrDefault();
				if (true)
				{
					TimeSpan delay = retryDate - DateTimeOffset.UtcNow;
					if (delay > TimeSpan.Zero)
					{
						return delay;
					}
				}
			}
		}
		double exponentialSeconds = Math.Pow(2.0, attempt + 3);
		int jitterMilliseconds = Random.Shared.Next(0, 2000);
		TimeSpan delayWithJitter = TimeSpan.FromSeconds(exponentialSeconds) + TimeSpan.FromMilliseconds(jitterMilliseconds);
		return (delayWithJitter > DefaultRateLimitDelay) ? delayWithJitter : DefaultRateLimitDelay;
	}
}
