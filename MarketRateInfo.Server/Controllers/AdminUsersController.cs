using MarketRateInfo.Server.BusinessLogic.Models.Requests;
using MarketRateInfo.Server.BusinessLogic.Models.Responses;
using MarketRateInfo.Server.DataAccess.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MarketRateInfo.Server.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public sealed class AdminUsersController(
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminUserResponse>>> GetUsers(
        CancellationToken cancellationToken)
    {
        var users = userManager.Users
            .OrderBy(x => x.Email)
            .ToList();

        var result = new List<AdminUserResponse>(users.Count);

        foreach (var user in users)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var roles = await userManager.GetRolesAsync(user);

            result.Add(new AdminUserResponse(
                user.Id,
                user.Email ?? string.Empty,
                roles.ToArray()));
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<AdminUserResponse>> CreateUser(
        [FromBody] AdminCreateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(request.ConfirmPassword), "Passwords do not match.");
            return ValidationProblem(ModelState);
        }

        var normalizedEmail = request.Email.Trim();

        if (await userManager.FindByEmailAsync(normalizedEmail) is not null)
            return Conflict(new { message = "A user with this email already exists." });

        var role = NormalizeRole(request.Role);

        if (role is null)
        {
            ModelState.AddModelError(nameof(request.Role), "Role must be Admin or User.");
            return ValidationProblem(ModelState);
        }

        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, request.Password);

        if (!createResult.Succeeded)
            return IdentityValidationProblem(createResult.Errors);

        var roleResult = await userManager.AddToRoleAsync(user, role);

        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return IdentityValidationProblem(roleResult.Errors);
        }

        return Ok(await ToResponseAsync(user));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AdminUserResponse>> UpdateUser(
        string id,
        [FromBody] AdminUpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = await userManager.FindByIdAsync(id);

        if (user is null)
            return NotFound(new { message = "User not found." });

        var currentUserId = userManager.GetUserId(User);
        var normalizedEmail = request.Email.Trim();

        var existingUser = await userManager.FindByEmailAsync(normalizedEmail);
        if (existingUser is not null && existingUser.Id != user.Id)
            return Conflict(new { message = "A user with this email already exists." });

        var newRole = NormalizeRole(request.Role);
        if (newRole is null)
        {
            ModelState.AddModelError(nameof(request.Role), "Role must be Admin or User.");
            return ValidationProblem(ModelState);
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var currentRole = currentRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase)
            ? "Admin"
            : "User";

        if (currentUserId == user.Id &&
            !string.Equals(newRole, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(request.Role), "You cannot remove the Admin role from your own account.");
            return ValidationProblem(ModelState);
        }

        if (string.Equals(currentRole, "Admin", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(newRole, "Admin", StringComparison.OrdinalIgnoreCase) &&
            await CountAdminsAsync() <= 1)
        {
            ModelState.AddModelError(nameof(request.Role), "The last administrator cannot be demoted.");
            return ValidationProblem(ModelState);
        }

        user.Email = normalizedEmail;
        user.UserName = normalizedEmail;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return IdentityValidationProblem(updateResult.Errors);

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(request.ConfirmPassword), "Passwords do not match.");
            return ValidationProblem(ModelState);
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            if (await userManager.HasPasswordAsync(user))
            {
                var removePasswordResult = await userManager.RemovePasswordAsync(user);
                if (!removePasswordResult.Succeeded)
                    return IdentityValidationProblem(removePasswordResult.Errors);
            }

            var addPasswordResult = await userManager.AddPasswordAsync(user, request.Password);
            if (!addPasswordResult.Succeeded)
                return IdentityValidationProblem(addPasswordResult.Errors);
        }

        if (!string.Equals(currentRole, newRole, StringComparison.OrdinalIgnoreCase))
        {
            var removeRolesResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeRolesResult.Succeeded)
                return IdentityValidationProblem(removeRolesResult.Errors);

            var addRoleResult = await userManager.AddToRoleAsync(user, newRole);
            if (!addRoleResult.Succeeded)
                return IdentityValidationProblem(addRoleResult.Errors);
        }

        return Ok(await ToResponseAsync(user));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(
        string id,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id);

        if (user is null)
            return NotFound(new { message = "User not found." });

        var currentUserId = userManager.GetUserId(User);

        if (currentUserId == user.Id)
            return BadRequest(new { message = "You cannot delete your own account." });

        if (await userManager.IsInRoleAsync(user, "Admin") && await CountAdminsAsync() <= 1)
            return BadRequest(new { message = "The last administrator cannot be deleted." });

        var result = await userManager.DeleteAsync(user);

        if (!result.Succeeded)
            return IdentityValidationProblem(result.Errors);

        return NoContent();
    }

    private async Task<AdminUserResponse> ToResponseAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);

        return new AdminUserResponse(
            user.Id,
            user.Email ?? string.Empty,
            roles.ToArray());
    }

    private async Task<int> CountAdminsAsync()
    {
        var admins = await userManager.GetUsersInRoleAsync("Admin");
        return admins.Count;
    }

    private static string? NormalizeRole(string? role)
    {
        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            return "Admin";

        if (string.Equals(role, "User", StringComparison.OrdinalIgnoreCase))
            return "User";

        return null;
    }

    private ActionResult IdentityValidationProblem(IEnumerable<IdentityError> errors)
    {
        foreach (var error in errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return ValidationProblem(ModelState);
    }
}

