using MarketRateInfo.Server.BusinessLogic.Models.Requests;
using MarketRateInfo.Server.BusinessLogic.Models.Responses;
using MarketRateInfo.Server.DataAccess.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MarketRateInfo.Server.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized(new { message = "Invalid email or password." });

        var result = await signInManager.PasswordSignInAsync(
            user,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        if (!result.Succeeded)
            return Unauthorized(new { message = "Invalid email or password." });

        return Ok(await CreateAuthResponseAsync(user));
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return ValidationProblem(ModelState);
        }

        var roleResult = await userManager.AddToRoleAsync(user, "User");

        if (!roleResult.Succeeded)
        {
            foreach (var error in roleResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return ValidationProblem(ModelState);
        }

        await signInManager.SignInAsync(user, isPersistent: false);

        return Ok(await CreateAuthResponseAsync(user));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthResponse>> Me()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        return Ok(await CreateAuthResponseAsync(user));
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);

        return new AuthResponse {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Roles = roles.ToArray()
        };
    }
}
