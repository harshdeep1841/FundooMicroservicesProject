namespace Fundoo.Notes.Domain.Entities;

public class Label
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<NoteLabel> NoteLabels { get; set; }
        = new List<NoteLabel>();
}
