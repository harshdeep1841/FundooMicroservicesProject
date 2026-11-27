namespace Fundoo.User.Api.Contracts;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public Guid SessionId { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
