using DataProvider.Data;

namespace DataProvider.Binary;

public sealed class RedisBinaryStorage : IBinaryStorage
{
    private const string KeyPrefix = "dataprovider:binary-storage:";
    private readonly IBinaryDataStore _dataStore;

    public RedisBinaryStorage(IBinaryDataStore dataStore)
    {
        ArgumentNullException.ThrowIfNull(dataStore);
        _dataStore = dataStore;
    }

    public async Task<string> WriteAsync(
        DateTimeOffset expiresAt,
        IBinaryContentWriter contentWriter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contentWriter);
        if (expiresAt <= DateTimeOffset.UtcNow)
            throw new ArgumentOutOfRangeException(nameof(expiresAt), "Expiration must be in the future.");

        using var content = new MemoryStream();
        string name;
        await using (var hashingStream = new Sha256WriteStream(content))
        {
            await contentWriter.WriteAsync(hashingStream, cancellationToken).ConfigureAwait(false);
            await hashingStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            name = hashingStream.GetHash();
        }

        cancellationToken.ThrowIfCancellationRequested();
        var timeToLive = expiresAt - DateTimeOffset.UtcNow;
        if (timeToLive <= TimeSpan.Zero)
            throw new InvalidOperationException("The requested expiration elapsed before the binary content could be stored.");

        var data = content.GetBuffer().AsMemory(0, checked((int)content.Length));
        await _dataStore.SetAsync(
            $"{KeyPrefix}{name}",
            data,
            timeToLive,
            cancellationToken).ConfigureAwait(false);

        return name;
    }

    public async Task<Stream?> OpenReadAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateName(name);

        var data = await _dataStore.GetAsync($"{KeyPrefix}{name}", cancellationToken).ConfigureAwait(false);
        return data is null ? null : new MemoryStream(data, writable: false);
    }

    private static void ValidateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Length > 128
            || name.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_'))
        {
            throw new ArgumentException("The storage name may contain only ASCII letters, digits, '-' and '_'.", nameof(name));
        }
    }
}
