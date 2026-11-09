using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fundoo.User.Domain.Entities;

public class ExternalLogin
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public AppUser User { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string Provider { get; set; } = "";

    [Required]
    [StringLength(256)]
    public string ProviderUserId { get; set; } = "";
}