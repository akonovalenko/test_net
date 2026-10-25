using System;

namespace MarketRateInfo.Server.BusinessLogic.Models.Responses;

public sealed class AuthResponse
{
	public string Id { get; set; } = string.Empty;

	public string Email { get; set; } = string.Empty;

	public string[] Roles { get; set; } = Array.Empty<string>();
}
