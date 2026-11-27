namespace Fundoo.User.Application.Models;

public enum SessionRefreshStatus
{
    Refreshed,
    IdempotencyConflict,
    Invalid,
    Expired,
    Revoked,
    Reused
}
