namespace Fundoo.Notes.Application.Interfaces;

public interface IUserDirectoryClient
{
    Task<Guid?> FindUserIdByEmailAsync(
        string email,
        CancellationToken cancellationToken);

    Task<bool> IsSessionActiveAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken);
}
