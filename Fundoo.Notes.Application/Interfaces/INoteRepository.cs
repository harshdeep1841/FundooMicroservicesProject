using Fundoo.Notes.Domain.Entities;

namespace Fundoo.Notes.Application.Interfaces;

public interface INoteRepository
{
    Task<Note?> GetByIdAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Note>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Note>> GetArchivedByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Note>> GetTrashedByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task AddAsync(
        Note note,
        CancellationToken cancellationToken);

    Task<bool> UpdateAsync(
        Note note,
        CancellationToken cancellationToken);

    Task<bool> SetArchiveStateAsync(
        Guid noteId,
        Guid userId,
        bool isArchived,
        CancellationToken cancellationToken);

    Task<bool> SetTrashStateAsync(
        Guid noteId,
        Guid userId,
        bool isTrashed,
        CancellationToken cancellationToken);

    Task<bool> PermanentlyDeleteAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken);
}