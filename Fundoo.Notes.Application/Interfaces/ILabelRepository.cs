using Fundoo.Notes.Domain.Entities;

namespace Fundoo.Notes.Application.Interfaces;

public interface ILabelRepository
{
    Task<Label?> GetByIdAsync(
        Guid labelId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Label>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task AddAsync(
        Label label,
        CancellationToken cancellationToken);

    Task<bool> UpdateAsync(
        Label label,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(
        Guid labelId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<bool> AttachLabelToNoteAsync(
        Guid noteId,
        Guid labelId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<bool> DetachLabelFromNoteAsync(
        Guid noteId,
        Guid labelId,
        Guid userId,
        CancellationToken cancellationToken);
}
