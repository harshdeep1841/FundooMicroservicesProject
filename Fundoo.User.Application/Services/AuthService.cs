
using System.Security.Cryptography;
using System.Text;
using Fundoo.User.Application.Interfaces;
using Fundoo.User.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Fundoo.User.Application.Services;

public class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    INotificationClient notificationClient,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<AppUser> RegisterAsync(
        AppUser user,
        string password,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = user.Email.Trim().ToUpperInvariant();

        var existingUser =
            await userRepository.GetByNormalizedEmailAsync(
                normalizedEmail,
                cancellationToken);

        if (existingUser is not null)
        {
            throw new InvalidOperationException(
                "An account with this email already exists.");
        }

        user.Email = user.Email.Trim();
        user.NormalizedEmail = normalizedEmail;
        user.PasswordHash = passwordHasher.Hash(password);

        user.CreatedAtUtc = DateTime.UtcNow;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await notificationClient.SendEmailAsync(
                user.Email,
                "Welcome to Fundoo",
                $"Hi {user.FirstName}, your account has been created successfully.",
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Welcome email delivery failed after registration.");
        }

        return user;
    }

    public async Task<string?> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();

        var user = await userRepository.GetByNormalizedEmailAsync(
            normalizedEmail,
            cancellationToken);

        if (user is null || !user.IsActive)
        {
            return null;
        }

        var isPasswordValid = passwordHasher.Verify(
            password,
            user.PasswordHash);

        if (!isPasswordValid)
        {
            return null;
        }

        return tokenService.CreateAccessToken(user);
    }

    public async Task ForgotPasswordAsync(
        string email,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();

        logger.LogInformation(
            "Processing forgot-password request.");

        var user = await userRepository.GetByNormalizedEmailAsync(
            normalizedEmail,
            cancellationToken);

        if (user is null || !user.IsActive)
        {
            logger.LogInformation(
                "No eligible account found for password reset.");

            return;
        }

        var rawToken = Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(32));

        var tokenHash = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(rawToken)));

        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15),
            UsedAtUtc = null
        };

        await userRepository.AddPasswordResetTokenAsync(
            resetToken,
            cancellationToken);

        await userRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Password-reset token saved. Attempting notification delivery.");

        try
        {
            await notificationClient.SendEmailAsync(
                user.Email,
                "Reset your Fundoo password",
                $"Your password-reset token is: {rawToken}\n\n" +
                "It expires in 15 minutes and can only be used once.",
                cancellationToken);

            logger.LogInformation(
                "Password-reset notification request completed.");
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Password-reset notification delivery failed.");

            throw;
        }
    }

    public async Task<bool> ResetPasswordAsync(
        string token,
        string newPassword,
        CancellationToken cancellationToken)
    {
        // Hash the raw token received from the client.
        var tokenHash = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(token)));

        var resetToken =
            await userRepository.GetPasswordResetTokenAsync(
                tokenHash,
                cancellationToken);

        if (resetToken is null)
            return false;

        if (resetToken.UsedAtUtc is not null)
            return false;

        if (resetToken.ExpiresAtUtc <= DateTime.UtcNow)
            return false;

        var user = await userRepository.GetByIdAsync(
            resetToken.UserId,
            cancellationToken);

        if (user is null || !user.IsActive)
            return false;

        // Hash the new password. Never store it as plain text.
        user.PasswordHash = passwordHasher.Hash(newPassword);
        user.UpdatedAtUtc = DateTime.UtcNow;

        // Make the token unusable after a successful reset.
        resetToken.UsedAtUtc = DateTime.UtcNow;

        // Persist the password update and token usage together.
        await userRepository.SaveChangesAsync(cancellationToken);

        return true;
    }
    
    
    public async Task<string?> GoogleLoginAsync(
    string googleUserId,
    string email,
    string? firstName,
    string? lastName,
    CancellationToken cancellationToken)
{
    // 1. Normalize email
    var normalizedEmail = email.Trim().ToUpperInvariant();

    // 2. Check whether this Google identity already exists
    var externalLogin =
        await userRepository.GetExternalLoginAsync(
            "Google",
            googleUserId,
            cancellationToken);

    AppUser? user;

    if (externalLogin is not null)
    {
        // Existing Google identity
        user = externalLogin.User;
    }
    else
    {
        // 3. Google identity doesn't exist.
        // Check whether a Fundoo account already exists
        // with the same email.
        user = await userRepository.GetByNormalizedEmailAsync(
            normalizedEmail,
            cancellationToken);

        if (user is null)
        {
            // 4. Register a new Fundoo user
            user = new AppUser
            {
                Id = Guid.NewGuid(),
                FirstName = firstName ?? "",
                LastName = lastName ?? "",
                Email = email.Trim(),
                NormalizedEmail = normalizedEmail,
                PasswordHash = "",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            await userRepository.AddAsync(
                user,
                cancellationToken);

            await userRepository.SaveChangesAsync(
                cancellationToken);
        }

        // 5. Link Google identity to the Fundoo user
        externalLogin = new ExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Provider = "Google",
            ProviderUserId = googleUserId
        };

        await userRepository.AddExternalLoginAsync(
            externalLogin,
            cancellationToken);

        await userRepository.SaveChangesAsync(
            cancellationToken);
    }

    // 6. Check whether Fundoo account is active
    if (!user.IsActive)
    {
        logger.LogWarning(
            "Google login rejected for inactive user {UserId}.",
            user.Id);

        return null;
    }

    // 7. Generate Fundoo JWT
    return tokenService.CreateAccessToken(user);
}

}
