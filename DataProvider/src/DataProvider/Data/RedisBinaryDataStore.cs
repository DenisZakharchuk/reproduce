using Microsoft.Extensions.Caching.Distributed;

namespace DataProvider.Data;

public class RedisBinaryDataStore : IBinaryDataStore
{
    private readonly IDistributedCache _cache;

    public RedisBinaryDataStore(IDistributedCache cache)
    {
        _cache = cache;
    }

    public Task SetAsync(
        string key,
        ReadOnlyMemory<byte> data,
        TimeSpan timeToLive,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeToLive, TimeSpan.Zero);

        return _cache.SetAsync(
            key,
            data.ToArray(),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = timeToLive,
            },
            cancellationToken);
    }

    public Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return _cache.GetAsync(key, cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return _cache.RemoveAsync(key, cancellationToken);
    }
}
