
using Fundoo.Notes.Application.Interfaces;
using Fundoo.Notes.Domain.Entities;

namespace Fundoo.Notes.Application.Services;

public class NoteService(
    INoteRepository noteRepository,
    INotesCache notesCache) : INoteService
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

        await notesCache.RemoveUserNotesAsync(
            userId,
            cancellationToken);

        return note;
    }

    public async Task<IReadOnlyList<Note>> GetMyNotesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var cachedNotes = await notesCache.GetUserNotesAsync(
            userId,
            cancellationToken);

        if (cachedNotes is not null)
            return cachedNotes;

        var notes = await noteRepository.GetAllByUserIdAsync(
            userId,
            cancellationToken);

        await notesCache.SetUserNotesAsync(
            userId,
            notes,
            cancellationToken);

        return notes;
    }

    public async Task<Note?> GetNoteByIdAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var cachedNote = await notesCache.GetNoteAsync(
            noteId,
            cancellationToken);

        if (cachedNote is not null)
            return cachedNote;

        var note = await noteRepository.GetByIdAsync(
            noteId,
            userId,
            cancellationToken);

        if (note is null)
            return null;

        await notesCache.SetNoteAsync(
            noteId,
            note,
            cancellationToken);

        return note;
    }

    public async Task<Note?> UpdateNoteAsync(
        Guid noteId,
        Guid userId,
        string title,
        string content,
        CancellationToken cancellationToken)
    {
        var note = await noteRepository.GetByIdAsync(
            noteId,
            userId,
            cancellationToken);

        if (note is null || note.IsTrashed)
            return null;

        note.Title = title.Trim();
        note.Content = content.Trim();
        note.UpdatedAtUtc = DateTime.UtcNow;

        var updated = await noteRepository.UpdateAsync(
            note,
            cancellationToken);

        if (!updated)
            return null;

        await notesCache.RemoveUserNotesAsync(
            userId,
            cancellationToken);

        return note;
    }

    public async Task<bool> ArchiveNoteAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var archived = await noteRepository.SetArchiveStateAsync(
            noteId,
            userId,
            true,
            cancellationToken);

        if (archived)
        {
            await notesCache.RemoveUserNotesAsync(
                userId,
                cancellationToken);
        }

        return archived;
    }

    public async Task<bool> UnarchiveNoteAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var unarchived = await noteRepository.SetArchiveStateAsync(
            noteId,
            userId,
            false,
            cancellationToken);

        if (unarchived)
        {
            await notesCache.RemoveUserNotesAsync(
                userId,
                cancellationToken);
        }

        return unarchived;
    }

    public async Task<bool> TrashNoteAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var trashed = await noteRepository.SetTrashStateAsync(
            noteId,
            userId,
            true,
            cancellationToken);

        if (trashed)
        {
            await notesCache.RemoveUserNotesAsync(
                userId,
                cancellationToken);
        }

        return trashed;
    }

    public async Task<bool> RestoreNoteAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var restored = await noteRepository.SetTrashStateAsync(
            noteId,
            userId,
            false,
            cancellationToken);

        if (restored)
        {
            await notesCache.RemoveUserNotesAsync(
                userId,
                cancellationToken);
        }

        return restored;
    }

    public async Task<bool> PermanentlyDeleteNoteAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var deleted = await noteRepository.PermanentlyDeleteAsync(
            noteId,
            userId,
            cancellationToken);

        if (deleted)
        {
            await notesCache.RemoveUserNotesAsync(
                userId,
                cancellationToken);
        }

        return deleted;
    }

    public Task<IReadOnlyList<Note>> GetArchivedNotesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return noteRepository.GetArchivedByUserIdAsync(
            userId,
            cancellationToken);
    }

    public Task<IReadOnlyList<Note>> GetTrashedNotesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return noteRepository.GetTrashedByUserIdAsync(
            userId,
            cancellationToken);
    }
}
