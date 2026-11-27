using System.ComponentModel.DataAnnotations;

namespace Fundoo.Notes.Api.Contracts;

public sealed class AddCollaboratorRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
