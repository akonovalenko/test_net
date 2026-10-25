using System.Globalization;

namespace MarketRateInfo.Server.BusinessLogic.Extensions;

/// <summary>
/// Provides helper methods for currency code normalization, validation, and date parsing for rate providers.
/// </summary>
internal static class RateProviderHelpers
{
	/// <summary>
	/// Contains the set of supported ISO currency codes.
	/// </summary>
	private static readonly HashSet<string> SupportedCurrencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"AED", "AUD", "BGN", "BRL", "CAD", "CHF", "CNY", "CZK", "DKK", "EUR",
		"GBP", "HKD", "HUF", "IDR", "ILS", "INR", "ISK", "JPY", "KRW", "MXN",
		"MYR", "NOK", "NZD", "PHP", "PLN", "RON", "SEK", "SGD", "THB", "TRY",
		"USD", "ZAR", "UAH", "RUB"
	};

	/// <summary>
	/// Determines whether the specified currency code represents a supported fiat currency.
	/// </summary>
	/// <param name="currency">The currency code to check.</param>
	/// <returns>true if the currency code is a supported fiat currency; otherwise, false.</returns>
	public static bool IsFiatCurrency(string currency)
	{
		return !string.IsNullOrWhiteSpace(currency) && SupportedCurrencies.Contains(currency);
	}

	/// <summary>
	/// Normalizes a currency code to uppercase and validates that it consists of exactly three ASCII letters.
	/// </summary>
	/// <param name="currency">The currency code to normalize.</param>
	/// <returns>The normalized three-letter uppercase currency code.</returns>
	/// <exception cref="ArgumentException">Thrown when the currency code does not contain exactly three ASCII letters.</exception>
	internal static string NormalizeCurrency(string currency)
	{
		string normalized = currency.Trim().ToUpperInvariant();
		if (normalized.Length != 3 || normalized.Any((char character) => (character < 'A' || character > 'Z') ? true : false))
		{
			throw new ArgumentException("Currency codes must contain exactly 3 ASCII letters.", "currency");
		}
		return normalized;
	}

	/// <summary>
	/// Parses a date string as a UTC DateTime or returns the current UTC time if parsing fails.
	/// </summary>
	/// <param name="date">A date string to parse, or null.</param>
	/// <returns>A UTC DateTime parsed from the input string, or the current UTC time if parsing fails.</returns>
	internal static DateTime ParseRateDateUtc(string? date)
	{
		if (DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
		{
			return parsed;
		}
		return DateTime.UtcNow;
	}
}
