namespace Fundoo.User.Domain.Entities;

public class UserSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DeviceName { get; set; } = "Unknown device";
    public string RefreshTokenHash { get; set; } = string.Empty;
    public string? PreviousRefreshTokenHash { get; set; }
    public DateTime? PreviousTokenRotatedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastUsedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime AbsoluteExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public int Version { get; set; }
}
