using System.Text.Json;
using Fundoo.Notes.Application.Interfaces;
using Fundoo.Notes.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;

namespace Fundoo.Notes.Infrastructure.Caching;

public class RedisNotesCache(
    IDistributedCache distributedCache) : INotesCache
{
    private static string GetCacheKey(Guid userId)
        => $"notes:user:{userId}";

    public async Task<IReadOnlyList<Note>?> GetUserNotesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var key = GetCacheKey(userId);

        var cachedData = await distributedCache.GetStringAsync(
            key,
            cancellationToken);

        if (cachedData is null)
            return null;

        return JsonSerializer.Deserialize<List<Note>>(cachedData);
    }

    public async Task SetUserNotesAsync(
        Guid userId,
        IReadOnlyList<Note> notes,
        CancellationToken cancellationToken)
    {
        var key = GetCacheKey(userId);

        var serializedData = JsonSerializer.Serialize(notes);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };

        await distributedCache.SetStringAsync(
            key,
            serializedData,
            options,
            cancellationToken);
    }

    public async Task RemoveUserNotesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var key = GetCacheKey(userId);

        await distributedCache.RemoveAsync(
            key,
            cancellationToken);
    }
}