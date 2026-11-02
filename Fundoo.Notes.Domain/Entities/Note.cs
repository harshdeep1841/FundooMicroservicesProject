namespace Fundoo.Notes.Domain.Entities;

public class Note
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public bool IsArchived { get; set; }

    public bool IsTrashed { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
    
    public ICollection<NoteLabel> NoteLabels { get; set; }
        = new List<NoteLabel>();
}