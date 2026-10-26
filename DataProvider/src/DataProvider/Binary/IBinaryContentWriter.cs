namespace DataProvider.Binary;

public interface IBinaryContentWriter
{
    Task WriteAsync(Stream destination, CancellationToken cancellationToken = default);
}
