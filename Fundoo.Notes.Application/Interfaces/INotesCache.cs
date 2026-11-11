using Fundoo.Notes.Domain.Entities;

namespace Fundoo.Notes.Application.Interfaces;

public interface INotesCache
{
    Task<IReadOnlyList<Note>?> GetUserNotesAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task SetUserNotesAsync(
        Guid userId,
        IReadOnlyList<Note> notes,
        CancellationToken cancellationToken);

    Task RemoveUserNotesAsync(
        Guid userId,
        CancellationToken cancellationToken);
}