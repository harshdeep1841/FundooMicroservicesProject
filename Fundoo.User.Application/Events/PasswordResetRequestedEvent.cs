namespace Fundoo.User.Application.Events;

public sealed record PasswordResetRequestedEvent(
    Guid EventId,
    string Email,
    string ResetToken,
    DateTime ExpiresAtUtc
);