using Microsoft.AspNetCore.Identity;

namespace MarketRateInfo.Server.DataAccess.Entities;

/// <summary>
/// Represents an application user entity that extends the IdentityUser class for authentication and authorization purposes.
/// </summary>
public sealed class ApplicationUser : IdentityUser
{
}
