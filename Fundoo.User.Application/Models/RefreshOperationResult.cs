namespace Fundoo.User.Application.Models;

public sealed record RefreshOperationResult(
    SessionRefreshStatus Status,
    byte[]? EncryptedResponse = null);