namespace Fundoo.User.Application.Interfaces;

public interface IRefreshOperationProtector
{
    byte[] Protect(byte[] plaintext, Guid sessionId, Guid operationId, string tokenHash);
    byte[] Unprotect(byte[] ciphertext, Guid sessionId, Guid operationId, string tokenHash);
}
