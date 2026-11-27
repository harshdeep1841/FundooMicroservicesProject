using Fundoo.User.Domain.Entities;
using Fundoo.User.Application.Models;

namespace Fundoo.User.Application.Interfaces;

public interface IAuthService
{
    Task<Guid?> FindUserIdByEmailAsync(string email, CancellationToken cancellationToken);

    Task<AppUser> RegisterAsync(
        AppUser user,
        string password,
        CancellationToken cancellationToken);

    Task<AuthSessionTokens?> LoginAsync(
        string email,
        string password,
        string deviceName,
        CancellationToken cancellationToken);

    Task<(SessionRefreshStatus Status, AuthSessionTokens? Tokens)> RefreshSessionAsync(
        string refreshToken,
        Guid operationId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<UserSessionSummary>> GetSessionsAsync(
        Guid userId,
        Guid currentSessionId,
        CancellationToken cancellationToken);

    Task<bool> RevokeSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken);


    Task<bool> IsSessionActiveAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken);
    
    
    Task ForgotPasswordAsync(
        string email,
        CancellationToken cancellationToken);
    
    Task<bool> ResetPasswordAsync(
        string token,
        string newPassword,
        CancellationToken cancellationToken);

    public Task<AuthSessionTokens?> GoogleLoginAsync(
        string googleUserId,
        string email,
        string? firstName,
        string? lastName,
        string deviceName,
        CancellationToken cancellationToken);

}
