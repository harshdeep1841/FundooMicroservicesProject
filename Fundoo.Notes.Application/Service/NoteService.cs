
using Fundoo.Notes.Application.Interfaces;
using Fundoo.Notes.Domain.Entities;

namespace Fundoo.Notes.Application.Services;

public class NoteService(
    INoteRepository noteRepository,
    INotesCache notesCache,
    IUserDirectoryClient userDirectoryClient ) : INoteService
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
        return await noteRepository.GetByIdAsync(
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
            noteId,
            userId,
            cancellationToken);

        if (note is null || note.IsTrashed)
            return null;

        var usersWithAccess = await noteRepository.GetUserIdsWithAccessToNoteAsync(
            noteId,
            cancellationToken);

        note.Title = title.Trim();
        note.Content = content.Trim();
        note.UpdatedAtUtc = DateTime.UtcNow;

        var updated = await noteRepository.UpdateAsync(
            note,
            userId,
            cancellationToken);

        if (!updated)
            return null;

        await InvalidateUserNoteListsAsync(usersWithAccess, userId, cancellationToken);

        return note;
    }

    public async Task<bool> ArchiveNoteAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var usersWithAccess = await noteRepository.GetUserIdsWithAccessToNoteAsync(noteId, cancellationToken);
        var archived = await noteRepository.SetArchiveStateAsync(
            noteId,
            userId,
            true,
            cancellationToken);

        if (archived)
        {
            await InvalidateUserNoteListsAsync(usersWithAccess, userId, cancellationToken);
        }

        return archived;
    }

    public async Task<bool> UnarchiveNoteAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var usersWithAccess = await noteRepository.GetUserIdsWithAccessToNoteAsync(noteId, cancellationToken);
        var unarchived = await noteRepository.SetArchiveStateAsync(
            noteId,
            userId,
            false,
            cancellationToken);

        if (unarchived)
        {
            await InvalidateUserNoteListsAsync(usersWithAccess, userId, cancellationToken);
        }

        return unarchived;
    }

    public async Task<bool> TrashNoteAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var usersWithAccess = await noteRepository.GetUserIdsWithAccessToNoteAsync(noteId, cancellationToken);
        var trashed = await noteRepository.SetTrashStateAsync(
            noteId,
            userId,
            true,
            cancellationToken);

        if (trashed)
        {
            await InvalidateUserNoteListsAsync(usersWithAccess, userId, cancellationToken);
        }

        return trashed;
    }

    public async Task<bool> RestoreNoteAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var usersWithAccess = await noteRepository.GetUserIdsWithAccessToNoteAsync(noteId, cancellationToken);
        var restored = await noteRepository.SetTrashStateAsync(
            noteId,
            userId,
            false,
            cancellationToken);

        if (restored)
        {
            await InvalidateUserNoteListsAsync(usersWithAccess, userId, cancellationToken);
        }

        return restored;
    }

    public async Task<bool> PermanentlyDeleteNoteAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var usersWithAccess = await noteRepository.GetUserIdsWithAccessToNoteAsync(noteId, cancellationToken);
        var deleted = await noteRepository.PermanentlyDeleteAsync(
            noteId,
            userId,
            cancellationToken);

        if (deleted)
        {
            await InvalidateUserNoteListsAsync(usersWithAccess, userId, cancellationToken);
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
    
    
    public async Task<Guid?> AddCollaboratorAsync(
        Guid noteId,
        Guid ownerId,
        string email,
        CancellationToken cancellationToken)
    {
        if (!await noteRepository.IsOwnedByUserAsync(
                noteId, ownerId, cancellationToken))
        {
            return null;
        }

        var collaboratorId =
            await userDirectoryClient.FindUserIdByEmailAsync(
                email.Trim(),
                cancellationToken);

        if (collaboratorId is null || collaboratorId == ownerId)
            return null;

        var added = await noteRepository.AddCollaboratorAsync(
            noteId,
            ownerId,
            collaboratorId.Value,
            cancellationToken);

        if (!added)
            return null;

        await notesCache.RemoveUserNotesAsync(ownerId, cancellationToken);
        await notesCache.RemoveUserNotesAsync(
            collaboratorId.Value,
            cancellationToken);

        return collaboratorId;
    }

    public Task<IReadOnlyList<Guid>?> GetCollaboratorsAsync(
        Guid noteId,
        Guid ownerId,
        CancellationToken cancellationToken)
    {
        return noteRepository.GetCollaboratorsAsync(
            noteId, ownerId, cancellationToken);
    }

    public async Task<bool> RemoveCollaboratorAsync(
        Guid noteId,
        Guid ownerId,
        Guid collaboratorId,
        CancellationToken cancellationToken)
    {
        var removed = await noteRepository.RemoveCollaboratorAsync(
            noteId,
            ownerId,
            collaboratorId,
            cancellationToken);

        if (removed)
        {
            await notesCache.RemoveUserNotesAsync(ownerId, cancellationToken);
            await notesCache.RemoveUserNotesAsync(
                collaboratorId,
                cancellationToken);
        }

        return removed;
    }

    private async Task InvalidateUserNoteListsAsync(
        IReadOnlyList<Guid> userIds,
        Guid requestingUserId,
        CancellationToken cancellationToken)
    {
        foreach (var userId in userIds.Append(requestingUserId).Distinct())
        {
            await notesCache.RemoveUserNotesAsync(userId, cancellationToken);
        }
    }
}
