using System.Security.Claims;
using Fundoo.User.Api.Contracts;
using Fundoo.User.Application.Interfaces;
using Fundoo.User.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fundoo.User.Api.Controllers;

[EnableRateLimiting("UserApiPolicy")]
[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
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
        var token = await authService.LoginAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (token is null)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
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
    
    
    [HttpGet("google-login")]
    public IActionResult GoogleLogin()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleComplete))
        };

        return Challenge(properties, "Google");
    }

    [AllowAnonymous]
    [HttpGet("google-complete")]
    public async Task<IActionResult> GoogleComplete(
        CancellationToken cancellationToken)
    {
        // Step 1: Authenticate using the Google authentication cookie
        var result = await HttpContext.AuthenticateAsync("GoogleCookie");

        if (!result.Succeeded || result.Principal is null)
        {
            return Unauthorized(new
            {
                message = "Google authentication failed."
            });
        }

        // Step 2: Extract claims from the authenticated principal
        var principal = result.Principal;

        var googleUserId =
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var email =
            principal.FindFirst(ClaimTypes.Email)?.Value;

        var firstName =
            principal.FindFirst(ClaimTypes.GivenName)?.Value;

        var lastName =
            principal.FindFirst(ClaimTypes.Surname)?.Value;

        // Step 3: Ensure required claims are available
        if (string.IsNullOrWhiteSpace(googleUserId) ||
            string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized(new
            {
                message = "Required Google claims are missing."
            });
        }

        // Step 4: Call AuthService
        var token = await authService.GoogleLoginAsync(
            googleUserId,
            email,
            firstName,
            lastName,
            cancellationToken);

        // Step 5: Check Fundoo login result
        if (token is null)
        {
            return Unauthorized(new
            {
                message = "Your Fundoo account is inactive."
            });
        }

        // Step 6: Return Fundoo JWT
        return Ok(new AuthResponse
        {
            AccessToken = token
        });
    }

}