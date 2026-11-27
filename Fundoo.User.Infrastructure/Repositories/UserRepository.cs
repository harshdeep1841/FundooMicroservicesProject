using Fundoo.User.Application.Interfaces;
using Fundoo.User.Application.Models;
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

    public async Task AddSessionAsync(
        UserSession session,
        CancellationToken cancellationToken)
    {
        await dbContext.UserSessions.AddAsync(session, cancellationToken);
    }

    public async Task<UserSession?> GetSessionByRefreshTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        // First, check the current and immediately previous token.
        var session = await dbContext.UserSessions
            .FirstOrDefaultAsync(
                item =>
                    item.RefreshTokenHash == tokenHash ||
                    item.PreviousRefreshTokenHash == tokenHash,
                cancellationToken);

        if (session is not null)
        {
            return session;
        }

        // If the token was rotated more than once, find its session
        // through the historical refresh operation.
        var sessionId = await dbContext.RefreshOperations
            .Where(operation => operation.PresentedTokenHash == tokenHash)
            .Select(operation => (Guid?)operation.SessionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (sessionId is null)
        {
            return null;
        }

        return await dbContext.UserSessions
            .FirstOrDefaultAsync(
                item => item.Id == sessionId.Value,
                cancellationToken);
    }    public async Task<SessionRefreshStatus> RotateRefreshTokenAsync(
        string currentTokenHash,
        string nextTokenHash,
        DateTime nowUtc,
        DateTime nextExpiresAtUtc,
        TimeSpan concurrentRefreshWindow,
        CancellationToken cancellationToken)
    {
        var session = await dbContext.UserSessions.FirstOrDefaultAsync(
            item => item.RefreshTokenHash == currentTokenHash ||
                    item.PreviousRefreshTokenHash == currentTokenHash,
            cancellationToken);

        if (session is null)
            return SessionRefreshStatus.Invalid;

        if (session.RevokedAtUtc is not null)
            return SessionRefreshStatus.Revoked;

        if (session.ExpiresAtUtc <= nowUtc || session.AbsoluteExpiresAtUtc <= nowUtc)
        {
            await RevokeSessionByIdAsync(session.Id, nowUtc, cancellationToken);
            return SessionRefreshStatus.Expired;
        }

        if (session.PreviousRefreshTokenHash == currentTokenHash)
        {
            var rotatedAt = session.PreviousTokenRotatedAtUtc;
            if (rotatedAt is not null && nowUtc - rotatedAt.Value <= concurrentRefreshWindow)
                return SessionRefreshStatus.Refreshed;

            await RevokeSessionByIdAsync(session.Id, nowUtc, cancellationToken);
            return SessionRefreshStatus.Reused;
        }

        session.PreviousRefreshTokenHash = session.RefreshTokenHash;
        session.PreviousTokenRotatedAtUtc = nowUtc;
        session.RefreshTokenHash = nextTokenHash;
        session.LastUsedAtUtc = nowUtc;
        session.ExpiresAtUtc = nextExpiresAtUtc < session.AbsoluteExpiresAtUtc
            ? nextExpiresAtUtc
            : session.AbsoluteExpiresAtUtc;
        session.Version++;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return SessionRefreshStatus.Refreshed;
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.Entry(session).State = EntityState.Detached;
            var latest = await dbContext.UserSessions.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == session.Id, cancellationToken);

            if (latest?.PreviousRefreshTokenHash != currentTokenHash)
                return SessionRefreshStatus.Invalid;

            var rotatedAt = latest.PreviousTokenRotatedAtUtc;
            if (rotatedAt is not null && nowUtc - rotatedAt.Value <= concurrentRefreshWindow)
                return SessionRefreshStatus.Refreshed;

            await RevokeSessionByIdAsync(latest.Id, nowUtc, cancellationToken);
            return SessionRefreshStatus.Reused;
        }
    }

    public async Task<bool> RevokeSessionAsync(
        Guid userId,
        Guid sessionId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
        => await dbContext.UserSessions
            .Where(session => session.Id == sessionId &&
                              session.UserId == userId &&
                              session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(session => session.RevokedAtUtc, nowUtc)
                .SetProperty(session => session.Version, session => session.Version + 1),
                cancellationToken) > 0;

    public async Task<IReadOnlyList<UserSession>> GetActiveSessionsAsync(
        Guid userId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
        => await dbContext.UserSessions.AsNoTracking()
            .Where(session => session.UserId == userId &&
                              session.RevokedAtUtc == null &&
                              session.ExpiresAtUtc > nowUtc &&
                              session.AbsoluteExpiresAtUtc > nowUtc)
            .OrderByDescending(session => session.LastUsedAtUtc)
            .ToArrayAsync(cancellationToken);

    public async Task<bool> IsSessionActiveAsync(
        Guid userId,
        Guid sessionId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var userIsActive = await dbContext.Users.AnyAsync(
            user => user.Id == userId && user.IsActive,
            cancellationToken);

        return userIsActive && await dbContext.UserSessions.AnyAsync(
            session => session.Id == sessionId &&
                       session.UserId == userId &&
                       session.RevokedAtUtc == null &&
                       session.ExpiresAtUtc > nowUtc &&
                       session.AbsoluteExpiresAtUtc > nowUtc,
            cancellationToken);
    }

    private async Task RevokeSessionByIdAsync(
        Guid sessionId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        await dbContext.UserSessions
            .Where(session => session.Id == sessionId && session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(session => session.RevokedAtUtc, nowUtc)
                .SetProperty(session => session.Version, session => session.Version + 1),
                cancellationToken);
    }
    
    public async Task<RefreshOperationResult> RotateRefreshTokenAsync(
    Guid operationId,
    string currentTokenHash,
    string nextTokenHash,
    byte[] encryptedResponse,
    DateTime nowUtc,
    DateTime nextExpiresAtUtc,
    DateTime operationExpiresAtUtc,
    CancellationToken cancellationToken)
{
    // First, look for an operation created by an earlier request.
    // This also allows us to find a retry if the session has rotated again.
    var existingOperation = await dbContext.RefreshOperations
        .AsNoTracking()
        .FirstOrDefaultAsync(
            operation => operation.PresentedTokenHash == currentTokenHash,
            cancellationToken);

    // Find the session either through the existing operation or
    // through its current/previous refresh-token hash.
    var sessionId = existingOperation?.SessionId;

    if (sessionId is null)
    {
        sessionId = await dbContext.UserSessions
            .AsNoTracking()
            .Where(session =>
                session.RefreshTokenHash == currentTokenHash ||
                session.PreviousRefreshTokenHash == currentTokenHash)
            .Select(session => (Guid?)session.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    if (sessionId is null)
    {
        return new RefreshOperationResult(SessionRefreshStatus.Invalid);
    }

    await using var transaction =
        await dbContext.Database.BeginTransactionAsync(cancellationToken);

    // Lock the session row. A second refresh request for this session
    // must wait until this transaction finishes.
    var session = await dbContext.UserSessions
        .FromSqlInterpolated(
            $"SELECT * FROM \"UserSessions\" WHERE \"Id\" = {sessionId.Value} FOR UPDATE")
        .SingleOrDefaultAsync(cancellationToken);

    if (session is null)
    {
        await transaction.RollbackAsync(cancellationToken);
        return new RefreshOperationResult(SessionRefreshStatus.Invalid);
    }

    // A revoked session must never be refreshed, even for an exact retry.
    if (session.RevokedAtUtc.HasValue)
    {
        await transaction.CommitAsync(cancellationToken);
        return new RefreshOperationResult(SessionRefreshStatus.Revoked);
    }

    // Check whether this refresh token has already been processed.
    var operation = await dbContext.RefreshOperations
        .FirstOrDefaultAsync(
            item =>
                item.SessionId == session.Id &&
                item.PresentedTokenHash == currentTokenHash,
            cancellationToken);

    if (operation is not null)
    {
        // Same operation ID and same presented token:
        // return the exact response produced by the first request.
        if (operation.Id == operationId &&
            operation.ExpiresAtUtc > nowUtc)
        {
            if (session.ExpiresAtUtc <= nowUtc ||
                session.AbsoluteExpiresAtUtc <= nowUtc)
            {
                session.RevokedAtUtc = nowUtc;
                session.Version++;

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new RefreshOperationResult(SessionRefreshStatus.Expired);
            }

            await transaction.CommitAsync(cancellationToken);

            return new RefreshOperationResult(
                SessionRefreshStatus.Refreshed,
                operation.EncryptedResponse);
        }

        // The same consumed token was submitted with a different
        // operation ID, or its idempotency record has expired.
        session.RevokedAtUtc = nowUtc;
        session.Version++;

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new RefreshOperationResult(SessionRefreshStatus.Reused);
    }

    // An operation ID must not be reused for another refresh request.
    var operationIdAlreadyUsed = await dbContext.RefreshOperations
        .AnyAsync(
            item => item.Id == operationId,
            cancellationToken);

    if (operationIdAlreadyUsed)
    {
        await transaction.CommitAsync(cancellationToken);

        return new RefreshOperationResult(
            SessionRefreshStatus.IdempotencyConflict);
    }

    // The token must still be the current token to perform a rotation.
    // A previous token without an operation record is treated as reuse.
    if (session.RefreshTokenHash != currentTokenHash)
    {
        session.RevokedAtUtc = nowUtc;
        session.Version++;

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new RefreshOperationResult(SessionRefreshStatus.Reused);
    }

    // Enforce both inactivity expiry and absolute session expiry.
    if (session.ExpiresAtUtc <= nowUtc ||
        session.AbsoluteExpiresAtUtc <= nowUtc)
    {
        session.RevokedAtUtc = nowUtc;
        session.Version++;

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new RefreshOperationResult(SessionRefreshStatus.Expired);
    }

    // Rotate the refresh token.
    session.PreviousRefreshTokenHash = session.RefreshTokenHash;
    session.PreviousTokenRotatedAtUtc = nowUtc;
    session.RefreshTokenHash = nextTokenHash;
    session.LastUsedAtUtc = nowUtc;

    // Never extend the session beyond its absolute expiry.
    session.ExpiresAtUtc =
        nextExpiresAtUtc < session.AbsoluteExpiresAtUtc
            ? nextExpiresAtUtc
            : session.AbsoluteExpiresAtUtc;

    session.Version++;

    // Save the idempotency operation in the SAME transaction.
    // If either save fails, neither change should be committed.
    dbContext.RefreshOperations.Add(new RefreshOperation
    {
        Id = operationId,
        SessionId = session.Id,
        PresentedTokenHash = currentTokenHash,
        EncryptedResponse = encryptedResponse,
        CreatedAtUtc = nowUtc,
        ExpiresAtUtc = operationExpiresAtUtc
    });

    await dbContext.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);

    return new RefreshOperationResult(
        SessionRefreshStatus.Refreshed,
        encryptedResponse);
}

}
