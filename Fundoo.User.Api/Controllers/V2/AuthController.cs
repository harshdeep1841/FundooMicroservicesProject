using Asp.Versioning;
using Fundoo.User.Api.Contracts;
using Fundoo.User.Application.Interfaces;
using Fundoo.User.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fundoo.User.Api.Controllers.V2;

[ApiController]
[ApiVersion(2.0)]
[Route("api/v{version:apiVersion}/auth")]
[EnableRateLimiting("UserApiPolicy")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim()
        };

        try
        {
            await authService.RegisterAsync(
                user,
                request.Password,
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                new { message = "Account created successfully (V2)." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var tokens = await authService.LoginAsync(
            request.Email,
            request.Password,
            Request.Headers["X-Device-Name"].FirstOrDefault()
                ?? Request.Headers["User-Agent"].FirstOrDefault()
                ?? "Unknown device",
            cancellationToken);

        if (tokens is null)
        {
            return Unauthorized(
                new { message = "Invalid email or password." });
        }

        return Ok(new AuthResponse
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            SessionId = tokens.SessionId,
            ExpiresAtUtc = tokens.ExpiresAtUtc
        });
    }
}
