using System.ComponentModel.DataAnnotations;

namespace Fundoo.Notes.Api.Contracts;

public class UpdateLabelRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
}
