using System.Net;
using System.Net.Http.Json;
using Fundoo.Notes.Application.Interfaces;

namespace Fundoo.Notes.Infrastructure.Services;

public sealed class UserDirectoryClient(HttpClient client)
    : IUserDirectoryClient
{
    public async Task<Guid?> FindUserIdByEmailAsync(
        string email,
        CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync(
            "internal/users/lookup",
            new { Email = email },
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<UserLookupResponse>(
                cancellationToken: cancellationToken);

        return result?.UserId;
    }

    public async Task<bool> IsSessionActiveAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(
            $"internal/users/{userId}/sessions/{sessionId}/active",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.NotFound)
            return false;

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SessionStatusResponse>(
            cancellationToken: cancellationToken);
        return result?.IsActive == true;
    }

    private sealed record UserLookupResponse(Guid UserId);
    private sealed record SessionStatusResponse(bool IsActive);
}
