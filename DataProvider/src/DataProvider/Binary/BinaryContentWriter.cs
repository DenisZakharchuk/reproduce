namespace DataProvider.Binary;

public sealed class BinaryContentWriter<T> : IBinaryContentWriter
{
    private readonly IBinarySerializer _serializer;
    private readonly IAsyncEnumerable<T> _items;

    public BinaryContentWriter(IBinarySerializer serializer, IAsyncEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(items);
        _serializer = serializer;
        _items = items;
    }

    public Task WriteAsync(Stream destination, CancellationToken cancellationToken = default)
        => _serializer.SerializeAsync(_items, destination, cancellationToken);
}
