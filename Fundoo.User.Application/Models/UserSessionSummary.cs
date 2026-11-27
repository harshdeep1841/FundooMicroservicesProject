namespace Fundoo.User.Application.Models;

public sealed record UserSessionSummary(
    Guid SessionId,
    string DeviceName,
    DateTime CreatedAtUtc,
    DateTime LastUsedAtUtc,
    DateTime ExpiresAtUtc,
    bool IsCurrent);
