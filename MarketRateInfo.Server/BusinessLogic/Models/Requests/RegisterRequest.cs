using System.ComponentModel.DataAnnotations;

namespace MarketRateInfo.Server.BusinessLogic.Models.Requests;

public sealed class RegisterRequest
{
	[Required]
	[EmailAddress]
	public string Email { get; set; } = string.Empty;

	[Required]
	[StringLength(100, MinimumLength = 8)]
	public string Password { get; set; } = string.Empty;

	[Required]
	[Compare("Password")]
	public string ConfirmPassword { get; set; } = string.Empty;
}
