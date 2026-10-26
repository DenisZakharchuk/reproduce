using System.Globalization;
using DataProvider.Configuration;
using Microsoft.Extensions.Options;

namespace DataProvider.Binary;

public sealed class FileBinaryStorage : IBinaryStorage
{
    private const string DataFileName = "payload.bin";
    private const string ExpirationFileName = "expires-at.txt";
    private readonly string _rootDirectory;

    public FileBinaryStorage(IOptions<FileBinaryStorageOptions> options, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);

        var configuredDirectory = options.Value.Directory;
        if (string.IsNullOrWhiteSpace(configuredDirectory))
            throw new InvalidOperationException("BinaryStorage:Directory must be configured.");

        _rootDirectory = Path.GetFullPath(Path.IsPathRooted(configuredDirectory)
            ? configuredDirectory
            : Path.Combine(environment.ContentRootPath, configuredDirectory));
    }

    public async Task<string> WriteAsync(
        DateTimeOffset expiresAt,
        IBinaryContentWriter contentWriter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contentWriter);
        if (expiresAt <= DateTimeOffset.UtcNow)
            throw new ArgumentOutOfRangeException(nameof(expiresAt), "Expiration must be in the future.");

        Directory.CreateDirectory(_rootDirectory);
        var temporaryDirectory = Path.Combine(
            _rootDirectory,
            $".{Guid.NewGuid():N}.tmp");
        Directory.CreateDirectory(temporaryDirectory);

        try
        {
            var dataPath = Path.Combine(temporaryDirectory, DataFileName);
            string cacheKey;
            await using (var stream = new FileStream(
                dataPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 128 * 1024,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await using var hashingStream = new Sha256WriteStream(stream);
                await contentWriter.WriteAsync(hashingStream, cancellationToken).ConfigureAwait(false);
                await hashingStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                cacheKey = hashingStream.GetHash();
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            await File.WriteAllTextAsync(
                Path.Combine(temporaryDirectory, ExpirationFileName),
                expiresAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
                cancellationToken).ConfigureAwait(false);

            var destinationDirectory = GetEntryDirectory(cacheKey);
            try
            {
                Directory.Move(temporaryDirectory, destinationDirectory);
            }
            catch (IOException) when (Directory.Exists(destinationDirectory))
            {
                var cachedExpiresAt = await ReadExpirationAsync(destinationDirectory, cancellationToken)
                    .ConfigureAwait(false);
                if (cachedExpiresAt <= DateTimeOffset.UtcNow)
                {
                    Directory.Delete(destinationDirectory, recursive: true);
                    try
                    {
                        Directory.Move(temporaryDirectory, destinationDirectory);
                    }
                    catch (IOException) when (Directory.Exists(destinationDirectory))
                    {
                    }
                }
            }

            return cacheKey;
        }
        finally
        {
            if (Directory.Exists(temporaryDirectory))
                Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    public async Task<Stream?> OpenReadAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateName(name);

        var entryDirectory = GetEntryDirectory(name);
        if (!Directory.Exists(entryDirectory))
            return null;

        var expiresAt = await ReadExpirationAsync(entryDirectory, cancellationToken).ConfigureAwait(false);

        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            Directory.Delete(entryDirectory, recursive: true);
            return null;
        }

        return new FileStream(
            Path.Combine(entryDirectory, DataFileName),
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read | FileShare.Delete,
            bufferSize: 128 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
    }

    internal async Task RemoveExpiredEntriesAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_rootDirectory))
            return;

        foreach (var entryDirectory in Directory.EnumerateDirectories(_rootDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Path.GetFileName(entryDirectory).StartsWith(".", StringComparison.Ordinal))
                continue;

            var expiresAt = await ReadExpirationAsync(entryDirectory, cancellationToken).ConfigureAwait(false);
            if (expiresAt <= DateTimeOffset.UtcNow && Directory.Exists(entryDirectory))
                Directory.Delete(entryDirectory, recursive: true);
        }
    }

    private static async Task<DateTimeOffset> ReadExpirationAsync(
        string entryDirectory,
        CancellationToken cancellationToken)
    {
        var name = Path.GetFileName(entryDirectory);
        var expirationText = await File.ReadAllTextAsync(
            Path.Combine(entryDirectory, ExpirationFileName),
            cancellationToken).ConfigureAwait(false);
        if (!DateTimeOffset.TryParse(
            expirationText,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var expiresAt))
        {
            throw new InvalidDataException($"The stored expiration for '{name}' is invalid.");
        }

        return expiresAt;
    }

    private string GetEntryDirectory(string name)
        => Path.Combine(_rootDirectory, name);

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
