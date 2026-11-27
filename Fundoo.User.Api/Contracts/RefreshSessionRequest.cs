using System.ComponentModel.DataAnnotations;

namespace Fundoo.User.Api.Contracts;

public sealed class RefreshSessionRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;

    [Required]
    public Guid OperationId { get; set; }
}