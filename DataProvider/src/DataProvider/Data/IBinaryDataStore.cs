namespace DataProvider.Data;

public interface IBinaryDataStore
{
    Task SetAsync(
        string key,
        ReadOnlyMemory<byte> data,
        TimeSpan timeToLive,
        CancellationToken cancellationToken = default);

    Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
