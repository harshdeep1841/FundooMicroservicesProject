using Fundoo.Notes.Domain.Entities;

namespace Fundoo.Notes.Application.Interfaces;

public interface ILabelService
{
    Task<Label> CreateLabelAsync(
        Guid userId,
        string name,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Label>> GetMyLabelsAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<Label?> GetLabelByIdAsync(
        Guid labelId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<Label?> UpdateLabelAsync(
        Guid labelId,
        Guid userId,
        string name,
        CancellationToken cancellationToken);

    Task<bool> DeleteLabelAsync(
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
