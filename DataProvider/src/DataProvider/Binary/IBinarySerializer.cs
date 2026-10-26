namespace DataProvider.Binary;

public interface IBinarySerializer
{
    Task SerializeAsync<T>(
        IAsyncEnumerable<T> items,
        Stream destination,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<T> DeserializeAsync<T>(
        Stream source,
        CancellationToken cancellationToken = default);
}

public interface IBinaryStorage
{
    Task<string> WriteAsync(
        DateTimeOffset expiresAt,
        IBinaryContentWriter contentWriter,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(
        string name,
        CancellationToken cancellationToken = default);
}