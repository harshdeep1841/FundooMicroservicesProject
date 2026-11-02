
using Fundoo.Notes.Application.Interfaces;
using Fundoo.Notes.Domain.Entities;

namespace Fundoo.Notes.Application.Services;

public class NoteService(INoteRepository noteRepository) : INoteService
{
    public async Task<Note> CreateNoteAsync(
        Guid userId,
        string title,
        string content,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var note = new Note
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title.Trim(),
            Content = content.Trim(),
            IsArchived = false,
            IsTrashed = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await noteRepository.AddAsync(note, cancellationToken);

        return note;
    }

    public Task<IReadOnlyList<Note>> GetMyNotesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return noteRepository.GetAllByUserIdAsync(
            userId, cancellationToken);
    }

    public Task<Note?> GetNoteByIdAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return noteRepository.GetByIdAsync(
            noteId, userId, cancellationToken);
    }

    public async Task<Note?> UpdateNoteAsync(
        Guid noteId,
        Guid userId,
        string title,
        string content,
        CancellationToken cancellationToken)
    {
        var note = await noteRepository.GetByIdAsync(
            noteId, userId, cancellationToken);

        if (note is null || note.IsTrashed)
            return null;

        note.Title = title.Trim();
        note.Content = content.Trim();
        note.UpdatedAtUtc = DateTime.UtcNow;

        var updated = await noteRepository.UpdateAsync(
            note, cancellationToken);

        return updated ? note : null;
    }

    public Task<bool> ArchiveNoteAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return noteRepository.SetArchiveStateAsync(
            noteId, userId, true, cancellationToken);
    }

    public Task<bool> UnarchiveNoteAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return noteRepository.SetArchiveStateAsync(
            noteId, userId, false, cancellationToken);
    }

    public Task<bool> TrashNoteAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return noteRepository.SetTrashStateAsync(
            noteId, userId, true, cancellationToken);
    }

    public Task<bool> RestoreNoteAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return noteRepository.SetTrashStateAsync(
            noteId, userId, false, cancellationToken);
    }

    public Task<bool> PermanentlyDeleteNoteAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return noteRepository.PermanentlyDeleteAsync(
            noteId, userId, cancellationToken);
    }
    
    public Task<IReadOnlyList<Note>> GetArchivedNotesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return noteRepository.GetArchivedByUserIdAsync(
            userId, cancellationToken);
    }

    public Task<IReadOnlyList<Note>> GetTrashedNotesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return noteRepository.GetTrashedByUserIdAsync(
            userId, cancellationToken);
    }
}
