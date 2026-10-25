namespace MarketRateInfo.Server.BusinessLogic.Models.Responses;

public sealed class ErrorResponse
{
	public int StatusCode { get; init; }

	public string Message { get; init; } = string.Empty;

	public string TraceId { get; init; } = string.Empty;

	public string? Detail { get; init; }
}
