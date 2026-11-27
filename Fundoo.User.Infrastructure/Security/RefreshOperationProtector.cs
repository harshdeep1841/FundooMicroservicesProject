using Fundoo.User.Application.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace Fundoo.User.Infrastructure.Security;

public sealed class RefreshOperationProtector : IRefreshOperationProtector
{
    private readonly IDataProtectionProvider _provider;

    public RefreshOperationProtector(
        IDataProtectionProvider provider)
    {
        _provider = provider;
    }

    public byte[] Protect(
        byte[] plaintext,
        Guid sessionId,
        Guid operationId,
        string tokenHash)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        var protector = CreateProtector(
            sessionId,
            operationId,
            tokenHash);

        return protector.Protect(plaintext);
    }

    public byte[] Unprotect(
        byte[] ciphertext,
        Guid sessionId,
        Guid operationId,
        string tokenHash)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        var protector = CreateProtector(
            sessionId,
            operationId,
            tokenHash);

        return protector.Unprotect(ciphertext);
    }

    private IDataProtector CreateProtector(
        Guid sessionId,
        Guid operationId,
        string tokenHash)
    {
        return _provider.CreateProtector(
            "Fundoo.RefreshOperation.v1",
            sessionId.ToString("N"),
            operationId.ToString("N"),
            tokenHash);
    }
}
