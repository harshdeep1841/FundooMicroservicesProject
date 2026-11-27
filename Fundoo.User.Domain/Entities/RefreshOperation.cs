namespace Fundoo.User.Domain.Entities;

public sealed class RefreshOperation
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string PresentedTokenHash { get; set; } = string.Empty;
    public byte[] EncryptedResponse { get; set; } = Array.Empty<byte>();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
