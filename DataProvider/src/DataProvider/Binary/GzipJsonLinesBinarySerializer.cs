using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace DataProvider.Binary;

public sealed class GzipJsonLinesBinarySerializer : IBinarySerializer
{
    private static readonly byte[] NewLine = [(byte)'\n'];
    private readonly JsonSerializerOptions _jsonOptions;

    public GzipJsonLinesBinarySerializer(JsonSerializerOptions? jsonOptions = null)
    {
        _jsonOptions = jsonOptions is null
            ? new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            }
            : new JsonSerializerOptions(jsonOptions);
    }

    public async Task SerializeAsync<T>(
        IAsyncEnumerable<T> items,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite)
            throw new ArgumentException("The destination stream must be writable.", nameof(destination));

        await using (var gzip = new GZipStream(destination, CompressionLevel.Fastest, leaveOpen: true))
        {
            await foreach (var item in items.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                await JsonSerializer.SerializeAsync(gzip, item, _jsonOptions, cancellationToken)
                    .ConfigureAwait(false);
                await gzip.WriteAsync(NewLine, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async IAsyncEnumerable<T> DeserializeAsync<T>(
        Stream source,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.CanRead)
            throw new ArgumentException("The source stream must be readable.", nameof(source));

        await using var gzip = new GZipStream(source, CompressionMode.Decompress, leaveOpen: true);
        using var reader = new StreamReader(
            gzip,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 16 * 1024,
            leaveOpen: true);

        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (line.Length == 0)
                throw new JsonException("JSON Lines data must not contain empty lines.");

            yield return JsonSerializer.Deserialize<T>(line, _jsonOptions)!;
        }
    }
}
