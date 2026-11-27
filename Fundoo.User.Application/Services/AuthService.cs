
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Fundoo.User.Application.Events;
using Fundoo.User.Application.Interfaces;
using Fundoo.User.Application.Models;
using Fundoo.User.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Fundoo.User.Application.Services;

public class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    INotificationClient notificationClient,
    IPasswordResetEventPublisher passwordResetEventPublisher,
    IRefreshOperationProtector refreshOperationProtector,
    ILogger<AuthService> logger) : IAuthService
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);
    private static readonly TimeSpan MaximumSessionLifetime = TimeSpan.FromDays(90);
    private static readonly TimeSpan ConcurrentRefreshWindow = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan RefreshOperationLifetime =
        TimeSpan.FromMinutes(2);
    public async Task<Guid?> FindUserIdByEmailAsync(
        string email,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        var user = await userRepository.GetByNormalizedEmailAsync(
            normalizedEmail,
            cancellationToken);

        return user?.Id;
    }

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

    public async Task<AuthSessionTokens?> LoginAsync(
        string email,
        string password,
        string deviceName,
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

        return await CreateSessionAsync(user, deviceName, cancellationToken);
    }

 public async Task<(SessionRefreshStatus Status, AuthSessionTokens? Tokens)>
    RefreshSessionAsync(
        string refreshToken,
        Guid operationId,
        CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(refreshToken) ||
        operationId == Guid.Empty)
    {
        return (SessionRefreshStatus.Invalid, null);
    }

    var now = DateTime.UtcNow;
    var currentTokenHash = HashToken(refreshToken);

    // Find the session associated with this refresh token.
    var session = await userRepository.GetSessionByRefreshTokenHashAsync(
        currentTokenHash,
        cancellationToken);

    if (session is null)
    {
        return (SessionRefreshStatus.Invalid, null);
    }

    // Ensure that the associated user still exists and is active.
    var user = await userRepository.GetByIdAsync(
        session.UserId,
        cancellationToken);

    if (user is null || !user.IsActive)
    {
        await userRepository.RevokeSessionAsync(
            session.UserId,
            session.Id,
            now,
            cancellationToken);

        return (SessionRefreshStatus.Revoked, null);
    }

    // Prepare the next refresh token.
    var nextRefreshToken = tokenService.CreateRefreshToken();
    var nextTokenHash = HashToken(nextRefreshToken);

    // Sliding inactivity expiry, capped by the absolute session expiry.
    var nextExpiresAtUtc = now.Add(RefreshTokenLifetime);

    if (nextExpiresAtUtc > session.AbsoluteExpiresAtUtc)
    {
        nextExpiresAtUtc = session.AbsoluteExpiresAtUtc;
    }

    // Prepare the response that will be returned to the client.
    var candidateTokens = new AuthSessionTokens(
        tokenService.CreateAccessToken(user, session.Id),
        nextRefreshToken,
        session.Id,
        nextExpiresAtUtc);

    // Serialize and encrypt the response before saving it.
    var serializedResponse = JsonSerializer.SerializeToUtf8Bytes(
        candidateTokens);

    var encryptedResponse = refreshOperationProtector.Protect(
        serializedResponse,
        session.Id,
        operationId,
        currentTokenHash);

    // Atomically rotate the refresh token and persist the operation.
    var result = await userRepository.RotateRefreshTokenAsync(
        operationId,
        currentTokenHash,
        nextTokenHash,
        encryptedResponse,
        now,
        nextExpiresAtUtc,
        now.Add(RefreshOperationLifetime),
        cancellationToken);

    if (result.Status != SessionRefreshStatus.Refreshed ||
        result.EncryptedResponse is null)
    {
        return (result.Status, null);
    }

    // Decrypt the response returned by the repository.
    // For a duplicate request, this is the original response
    // saved by the first request, not the newly generated candidate.
    try
    {
        var decryptedResponse = refreshOperationProtector.Unprotect(
            result.EncryptedResponse,
            session.Id,
            operationId,
            currentTokenHash);

        var tokens = JsonSerializer.Deserialize<AuthSessionTokens>(
            decryptedResponse);

        if (tokens is null)
        {
            logger.LogError(
                "Unable to deserialize refresh response for session {SessionId}",
                session.Id);

            return (SessionRefreshStatus.Invalid, null);
        }

        return (SessionRefreshStatus.Refreshed, tokens);
    }
    catch (System.Security.Cryptography.CryptographicException exception)
    {
        logger.LogError(
            exception,
            "Unable to decrypt refresh response for session {SessionId}",
            session.Id);

        return (SessionRefreshStatus.Invalid, null);
    }
}

    public async Task<IReadOnlyList<UserSessionSummary>> GetSessionsAsync(
        Guid userId,
        Guid currentSessionId,
        CancellationToken cancellationToken)
    {
        var sessions = await userRepository.GetActiveSessionsAsync(
            userId, DateTime.UtcNow, cancellationToken);

        return sessions.Select(session => new UserSessionSummary(
            session.Id,
            session.DeviceName,
            session.CreatedAtUtc,
            session.LastUsedAtUtc,
            session.ExpiresAtUtc,
            session.Id == currentSessionId)).ToArray();
    }

    public Task<bool> RevokeSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken)
        => userRepository.RevokeSessionAsync(
            userId, sessionId, DateTime.UtcNow, cancellationToken);


    public Task<bool> IsSessionActiveAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken)
        => userRepository.IsSessionActiveAsync(
            userId, sessionId, DateTime.UtcNow, cancellationToken);

    private async Task<AuthSessionTokens> CreateSessionAsync(
        AppUser user,
        string deviceName,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var sessionId = Guid.NewGuid();
        var refreshToken = tokenService.CreateRefreshToken();
        var expiresAt = now.Add(RefreshTokenLifetime);
        var normalizedDeviceName = string.IsNullOrWhiteSpace(deviceName)
            ? "Unknown device"
            : deviceName.Trim();

        if (normalizedDeviceName.Length > 256)
            normalizedDeviceName = normalizedDeviceName[..256];

        var session = new UserSession
        {
            Id = sessionId,
            UserId = user.Id,
            DeviceName = normalizedDeviceName,
            RefreshTokenHash = HashToken(refreshToken),
            CreatedAtUtc = now,
            LastUsedAtUtc = now,
            ExpiresAtUtc = expiresAt,
            AbsoluteExpiresAtUtc = now.Add(MaximumSessionLifetime),
            Version = 1
        };

        await userRepository.AddSessionAsync(session, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        return new AuthSessionTokens(
            tokenService.CreateAccessToken(user, sessionId),
            refreshToken,
            sessionId,
            expiresAt);
    }

    private static string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

  
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
            "Password-reset token saved. Publishing notification event.");

        try
        {
            var passwordResetEvent = new PasswordResetRequestedEvent(
                Guid.NewGuid(),
                user.Email,
                rawToken,
                resetToken.ExpiresAtUtc
            );

            await passwordResetEventPublisher.PublishAsync(
                passwordResetEvent,
                cancellationToken);

            logger.LogInformation(
                "Password-reset notification event published successfully.");
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
                "Failed to publish password-reset notification event.");

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
    
    
    public async Task<AuthSessionTokens?> GoogleLoginAsync(
    string googleUserId,
    string email,
    string? firstName,
    string? lastName,
    string deviceName,
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
    return await CreateSessionAsync(user, deviceName, cancellationToken);
}

}
