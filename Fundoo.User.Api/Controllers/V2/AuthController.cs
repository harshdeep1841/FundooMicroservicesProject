
using Fundoo.User.Api.Contracts;
using Fundoo.User.Application.Interfaces;
using Fundoo.User.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fundoo.User.Api.Controllers.V2;

[EnableRateLimiting("UserApiPolicy")]
[ApiController]
[Route("api/v2/auth")]
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
                new { message = "Account created successfully." });
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
        var token = await authService.LoginAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (token is null)
        {
            return Unauthorized(
                new { message = "Invalid email or password." });
        }

        return Ok(new AuthResponse
        {
            AccessToken = token
        });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await authService.ForgotPasswordAsync(
            request.Email,
            cancellationToken);

        return Ok(new
        {
            message = "If an account exists, password reset instructions will be sent."
        });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var succeeded = await authService.ResetPasswordAsync(
            request.Token,
            request.NewPassword,
            cancellationToken);

        if (!succeeded)
        {
            return BadRequest(
                new { message = "The reset token is invalid or has expired." });
        }

        return Ok(
            new { message = "Password reset successfully." });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken(
        CancellationToken cancellationToken)
    {
        // Refresh-token implementation will be added next.
        await Task.CompletedTask;

        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        CancellationToken cancellationToken)
    {
        // Refresh-token revocation will be implemented next.
        await Task.CompletedTask;

        return StatusCode(StatusCodes.Status501NotImplemented);
    }
}
