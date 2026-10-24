using MarketRateInfo.Server.DataAccess.Entities;
using Microsoft.AspNetCore.Identity;

namespace MarketRateInfo.Server.DataAccess;

/// <summary>
/// Service responsible for seeding initial identity data, such as roles and a default admin user, into the database.
/// </summary>
/// <param name="roleManager">The role manager.</param>
/// <param name="userManager">The user manager.</param>
/// <param name="configuration">The configuration.</param>
/// <param name="logger">The logger.</param>
public sealed class IdentitySeedService(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    ILogger<IdentitySeedService> logger)
{
    public const string AdminRole = "Admin";
    public const string UserRole = "User";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRoleAsync(AdminRole);
        await EnsureRoleAsync(UserRole);

        var email = configuration["Identity:Admin:Email"];
        var password = configuration["Identity:Admin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Default admin was not created because Identity:Admin:Email or Identity:Admin:Password is not configured.");
            return;
        }

        var admin = await userManager.FindByEmailAsync(email);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, password);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to create default admin: {string.Join("; ", result.Errors.Select(x => x.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(admin, AdminRole))
        {
            var result = await userManager.AddToRoleAsync(admin, AdminRole);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to assign Admin role: {string.Join("; ", result.Errors.Select(x => x.Description))}");
            }
        }
    }

    /// <summary>
    /// Ensures that a role exists in the identity system. If the role does not exist, it will be created.
    /// </summary>
    /// <param name="role">The role to ensure.</param>
    /// <returns>The task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task EnsureRoleAsync(string role)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(role));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to create role '{role}': {string.Join("; ", result.Errors.Select(x => x.Description))}");
            }
        }
    }
}
