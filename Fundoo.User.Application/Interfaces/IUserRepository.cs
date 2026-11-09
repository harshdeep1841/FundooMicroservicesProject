using Fundoo.User.Domain.Entities;

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
}