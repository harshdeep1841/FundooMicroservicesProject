using Fundoo.User.Application.Interfaces;
using Fundoo.User.Domain.Entities;
using Fundoo.User.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fundoo.User.Infrastructure.Repositories;

public class UserRepository(UserDbContext dbContext)
    : IUserRepository
{
    public async Task<AppUser?> GetByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .FirstOrDefaultAsync(
                user => user.NormalizedEmail == normalizedEmail,
                cancellationToken);
    }

    public async Task<AppUser?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .FirstOrDefaultAsync(
                user => user.Id == userId,
                cancellationToken);
    }

    public async Task AddAsync(
        AppUser user,
        CancellationToken cancellationToken)
    {
        await dbContext.Users.AddAsync(user, cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
    
    
    public async Task AddPasswordResetTokenAsync(
        PasswordResetToken token,
        CancellationToken cancellationToken)
    {
        await dbContext.PasswordResetTokens.AddAsync(
            token,
            cancellationToken);
    }

    public async Task<PasswordResetToken?> GetPasswordResetTokenAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        return await dbContext.PasswordResetTokens
            .FirstOrDefaultAsync(
                token => token.TokenHash == tokenHash,
                cancellationToken);
    }
    
    public async Task<ExternalLogin?> GetExternalLoginAsync(
        string provider,
        string providerUserId,
        CancellationToken cancellationToken)
    {
        return await dbContext.ExternalLogins
            .Include(externalLogin => externalLogin.User)
            .FirstOrDefaultAsync(
                externalLogin =>
                    externalLogin.Provider == provider &&
                    externalLogin.ProviderUserId == providerUserId,
                cancellationToken);
    }

    public async Task AddExternalLoginAsync(
        ExternalLogin externalLogin,
        CancellationToken cancellationToken)
    {
        await dbContext.ExternalLogins.AddAsync(
            externalLogin,
            cancellationToken);
    }
}