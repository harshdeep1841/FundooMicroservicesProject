namespace Fundoo.Notes.Domain.Entities;

public class NoteCollaborator
{
    public Guid NoteId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}