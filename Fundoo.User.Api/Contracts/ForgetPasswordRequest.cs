using System.ComponentModel.DataAnnotations;

namespace Fundoo.User.Api.Contracts;

public class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
