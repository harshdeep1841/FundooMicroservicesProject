using Fundoo.User.Domain.Entities;
using Fundoo.User.Application.Models;

namespace Fundoo.User.Application.Interfaces;

public interface IUserRepository
{
    Task<AppUser?> GetByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken);

    Task<AppUser?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task AddAsync(
        AppUser user,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
    
    Task AddPasswordResetTokenAsync(
        PasswordResetToken token,
        CancellationToken cancellationToken);

    Task<PasswordResetToken?> GetPasswordResetTokenAsync(
        string tokenHash,
        CancellationToken cancellationToken);
    
    Task<ExternalLogin?> GetExternalLoginAsync(
        string provider,
        string providerUserId,
        CancellationToken cancellationToken);

    Task AddExternalLoginAsync(
        ExternalLogin externalLogin,
        CancellationToken cancellationToken);

    Task AddSessionAsync(UserSession session, CancellationToken cancellationToken);
    Task<UserSession?> GetSessionByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task<SessionRefreshStatus> RotateRefreshTokenAsync(
        string currentTokenHash,
        string nextTokenHash,
        DateTime nowUtc,
        DateTime nextExpiresAtUtc,
        TimeSpan concurrentRefreshWindow,
        CancellationToken cancellationToken);
    Task<bool> RevokeSessionAsync(Guid userId, Guid sessionId, DateTime nowUtc, CancellationToken cancellationToken);
    Task<IReadOnlyList<UserSession>> GetActiveSessionsAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken);
    Task<bool> IsSessionActiveAsync(Guid userId, Guid sessionId, DateTime nowUtc, CancellationToken cancellationToken);
    
    Task<RefreshOperationResult> RotateRefreshTokenAsync(
        Guid operationId,
        string currentTokenHash,
        string nextTokenHash,
        byte[] encryptedResponse,
        DateTime nowUtc,
        DateTime nextExpiresAtUtc,
        DateTime operationExpiresAtUtc,
        CancellationToken cancellationToken);
}
