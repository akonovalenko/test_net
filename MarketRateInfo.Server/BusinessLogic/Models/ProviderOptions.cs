namespace MarketRateInfo.Server.BusinessLogic.Configuration;

public sealed class ProviderOptions
{
	public bool Enabled { get; set; } = true;

	public string BaseUrl { get; set; } = string.Empty;

	public string ApiKey { get; set; } = string.Empty;

	public int TimeoutSeconds { get; set; } = 10;
}
