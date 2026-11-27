namespace Fundoo.User.Application.Models;

public sealed record AuthSessionTokens(
    string AccessToken,
    string RefreshToken,
    Guid SessionId,
    DateTime ExpiresAtUtc);
