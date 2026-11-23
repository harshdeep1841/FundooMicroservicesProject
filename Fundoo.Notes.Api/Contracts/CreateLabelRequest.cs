using System.ComponentModel.DataAnnotations;

namespace Fundoo.Notes.Api.Contracts;

public class CreateLabelRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
}
