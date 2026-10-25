namespace MarketRateInfo.Server.BusinessLogic.Configuration;

public sealed class MarketRateProviderOptions
{
	public ProviderOptions Frankfurter { get; set; } = new ProviderOptions
	{
		BaseUrl = "https://api.frankfurter.dev/"
	};

	public ProviderOptions ExchangeRateApi { get; set; } = new ProviderOptions
	{
		BaseUrl = "https://open.er-api.com/"
	};

	public ProviderOptions CryptoRateApi { get; set; } = new ProviderOptions
	{
		BaseUrl = "https://api.coingecko.com/api/v3/coins/bitcoin/history?date={0}"
	};
}
