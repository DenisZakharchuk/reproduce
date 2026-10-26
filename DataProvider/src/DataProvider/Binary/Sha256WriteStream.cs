using System.Security.Cryptography;

namespace DataProvider.Binary;

internal sealed class Sha256WriteStream : Stream
{
    private readonly Stream _inner;
    private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    private bool _disposed;

    public Sha256WriteStream(Stream inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        if (!inner.CanWrite)
            throw new ArgumentException("The wrapped stream must be writable.", nameof(inner));
        _inner = inner;
    }

    public string GetHash()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Convert.ToHexString(_hash.GetHashAndReset()).ToLowerInvariant();
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => !_disposed && _inner.CanWrite;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
        ThrowIfDisposed();
        _inner.Flush();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        return _inner.FlushAsync(cancellationToken);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        _inner.Write(buffer, offset, count);
        _hash.AppendData(buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ThrowIfDisposed();
        _inner.Write(buffer);
        _hash.AppendData(buffer);
    }

    public override async Task WriteAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        await _inner.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
        _hash.AppendData(buffer, offset, count);
    }

    public override async ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _inner.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        _hash.AppendData(buffer.Span);
    }

    public override void WriteByte(byte value)
    {
        ThrowIfDisposed();
        _inner.WriteByte(value);
        Span<byte> buffer = stackalloc byte[1];
        buffer[0] = value;
        _hash.AppendData(buffer);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _hash.Dispose();
            _disposed = true;
        }

        base.Dispose(disposing);
    }

    public override ValueTask DisposeAsync()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    public override int Read(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin)
        => throw new NotSupportedException();

    public override void SetLength(long value)
        => throw new NotSupportedException();

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_disposed, this);
}
