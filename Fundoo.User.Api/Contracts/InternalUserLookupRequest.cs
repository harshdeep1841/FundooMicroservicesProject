using System.ComponentModel.DataAnnotations;

namespace Fundoo.User.Api.Contracts;

public sealed class InternalUserLookupRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
