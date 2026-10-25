using System.ComponentModel.DataAnnotations;

namespace MarketRateInfo.Server.BusinessLogic.Models.Requests;

public sealed class AdminUpdateUserRequest
{
	[Required]
	[EmailAddress]
	public string Email { get; set; } = string.Empty;

	[StringLength(100, MinimumLength = 8)]
	public string? Password { get; set; }

	public string? ConfirmPassword { get; set; }

	[Required]
	public string Role { get; set; } = "User";
}
