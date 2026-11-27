using System.Security.Claims;
using Fundoo.User.Api.Contracts;
using Fundoo.User.Application.Interfaces;
using Fundoo.User.Application.Models;
using Fundoo.User.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fundoo.User.Api.Controllers;

[EnableRateLimiting("UserApiPolicy")]
[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService, ILogger<AuthController> logger) : ControllerBase
{
    
    
    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId is null)
            return Unauthorized();

        return Ok(new
        {
            UserId = userId
        });
    }
    
    
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

            return StatusCode(StatusCodes.Status201Created,
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
        var tokens = await authService.LoginAsync(
            request.Email,
            request.Password,
            GetDeviceName(),
            cancellationToken);

        if (tokens is null)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        return Ok(new AuthResponse
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            SessionId = tokens.SessionId,
            ExpiresAtUtc = tokens.ExpiresAtUtc
        });
    }

   
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        RefreshSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.OperationId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "A valid operation ID is required."
            });
        }

        var (status, tokens) = await authService.RefreshSessionAsync(
            request.RefreshToken,
            request.OperationId,
            cancellationToken);

        if (tokens is not null)
            return Ok(ToAuthResponse(tokens));

        var message = status switch
        {
            SessionRefreshStatus.Expired =>
                "Your session expired. Sign in again.",

            SessionRefreshStatus.Revoked =>
                "This session has been ended. Sign in again.",

            SessionRefreshStatus.Reused =>
                "A reused refresh token ended this session. Sign in again.",

            SessionRefreshStatus.IdempotencyConflict =>
                "The operation ID was already used with a different request.",

            _ =>
                "The refresh token is invalid. Sign in again."
        };

        return status == SessionRefreshStatus.IdempotencyConflict
            ? Conflict(new { message })
            : Unauthorized(new { message });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId) ||
            !TryGetCurrentSessionId(out var sessionId))
            return Unauthorized();

        await authService.RevokeSessionAsync(userId, sessionId, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId) ||
            !TryGetCurrentSessionId(out var sessionId))
            return Unauthorized();

        var sessions = await authService.GetSessionsAsync(
            userId, sessionId, cancellationToken);
        return Ok(sessions);
    }

    [Authorize]
    [HttpDelete("sessions/{sessionId:guid}")]
    public async Task<IActionResult> RevokeSession(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var revoked = await authService.RevokeSessionAsync(
            userId, sessionId, cancellationToken);
        return revoked ? NoContent() : NotFound();
    }
    
    
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        
        logger.LogInformation(
            "Forgot password request received.");
        await authService.ForgotPasswordAsync(
            request.Email, cancellationToken);

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
            return BadRequest(new
            {
                message = "The reset token is invalid or has expired."
            });
        }

        return Ok(new
        {
            message = "Password reset successfully."
        });
    }
    
    
    [AllowAnonymous]
    [HttpGet("google-login")]
    public IActionResult GoogleLogin()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = "/api/auth/google-complete"
        };

        return Challenge(properties, "Google");
    }
    
    [AllowAnonymous]
    [HttpGet("google-complete")]
    public async Task<IActionResult> GoogleComplete(
        CancellationToken cancellationToken)
    {
        var result = await HttpContext.AuthenticateAsync("GoogleCookie");

        if (!result.Succeeded || result.Principal is null)
        {
            return Unauthorized(new
            {
                message = "Google authentication failed."
            });
        }

        var principal = result.Principal;

        var googleUserId =
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var email =
            principal.FindFirst(ClaimTypes.Email)?.Value;

        var firstName =
            principal.FindFirst(ClaimTypes.GivenName)?.Value;

        var lastName =
            principal.FindFirst(ClaimTypes.Surname)?.Value;

        if (string.IsNullOrWhiteSpace(googleUserId) ||
            string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized(new
            {
                message = "Required Google claims are missing."
            });
        }

        var tokens = await authService.GoogleLoginAsync(
            googleUserId,
            email,
            firstName,
            lastName,
            GetDeviceName(),
            cancellationToken);

        if (tokens is null)
        {
            return Unauthorized(new
            {
                message = "Your Fundoo account is inactive."
            });
        }

        return Ok(new AuthResponse
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            SessionId = tokens.SessionId,
            ExpiresAtUtc = tokens.ExpiresAtUtc
        });
    }

    private string GetDeviceName()
        => Request.Headers["X-Device-Name"].FirstOrDefault()
            ?? Request.Headers["User-Agent"].FirstOrDefault()
            ?? "Unknown device";

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var value = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out userId);
    }

    private bool TryGetCurrentSessionId(out Guid sessionId)
    {
        var value = User.FindFirst("sid")?.Value
            ?? User.FindFirst(ClaimTypes.Sid)?.Value;
        return Guid.TryParse(value, out sessionId);
    }

    private static AuthResponse ToAuthResponse(AuthSessionTokens tokens)
        => new()
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            SessionId = tokens.SessionId,
            ExpiresAtUtc = tokens.ExpiresAtUtc
        };

}
