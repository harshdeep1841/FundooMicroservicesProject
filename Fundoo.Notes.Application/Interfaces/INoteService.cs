using Fundoo.Notes.Domain.Entities;

namespace Fundoo.Notes.Application.Interfaces;

public interface INoteService
{
    Task<Note> CreateNoteAsync(
        Guid userId,
        string title,
        string content,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Note>> GetMyNotesAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<Note?> GetNoteByIdAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken);
    
    Task<Note?> UpdateNoteAsync(
        Guid noteId,
        Guid userId,
        string title,
        string content,
        CancellationToken cancellationToken);
    
    
    Task<bool> ArchiveNoteAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<bool> UnarchiveNoteAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<bool> TrashNoteAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<bool> RestoreNoteAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken);
    
    Task<bool> PermanentlyDeleteNoteAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken);

    public Task<IReadOnlyList<Note>> GetArchivedNotesAsync(
        Guid userId,
        CancellationToken cancellationToken);


    public Task<IReadOnlyList<Note>> GetTrashedNotesAsync(
        Guid userId,
        CancellationToken cancellationToken);
}