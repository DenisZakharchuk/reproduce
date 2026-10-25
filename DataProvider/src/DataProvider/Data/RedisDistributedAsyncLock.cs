using StackExchange.Redis;

namespace DataProvider.Data;

public class RedisDistributedAsyncLock : IDistributedAsyncLock
{
    private const string KeyPrefix = "dataprovider:async-lock:";
    private const string ReleaseScript =
        "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";
    private const string RenewScript =
        "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('pexpire', KEYS[1], ARGV[2]) else return 0 end";
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(100);

    private readonly IDatabase _database;

    public RedisDistributedAsyncLock(IConnectionMultiplexer connectionMultiplexer)
    {
        _database = connectionMultiplexer.GetDatabase();
    }

    public async Task<IDistributedLockLease> AcquireAsync(
        string key,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        ValidateArguments(key, leaseDuration);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var lease = await TryAcquireCoreAsync(key, leaseDuration, cancellationToken);
            if (lease is not null)
            {
                return lease;
            }

            await Task.Delay(RetryDelay, cancellationToken);
        }
    }

    public Task<IDistributedLockLease?> TryAcquireAsync(
        string key,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        ValidateArguments(key, leaseDuration);
        cancellationToken.ThrowIfCancellationRequested();

        return TryAcquireCoreAsync(key, leaseDuration, cancellationToken);
    }

    private async Task<IDistributedLockLease?> TryAcquireCoreAsync(
        string key,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        var lockKey = $"{KeyPrefix}{key}";
        var ownerToken = Guid.NewGuid().ToString("N");
        var acquired = await _database.StringSetAsync(
            lockKey,
            ownerToken,
            leaseDuration,
            When.NotExists);

        if (!acquired)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            await ReleaseAsync(_database, lockKey, ownerToken);
            cancellationToken.ThrowIfCancellationRequested();
        }

        return new RedisLockLease(_database, lockKey, ownerToken, leaseDuration);
    }

    private static void ValidateArguments(string key, TimeSpan leaseDuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(leaseDuration, TimeSpan.Zero);
    }

    private static Task<RedisResult> ReleaseAsync(IDatabase database, RedisKey key, RedisValue ownerToken)
        => database.ScriptEvaluateAsync(
            ReleaseScript,
            new[] { key },
            new[] { ownerToken });

    private static Task<RedisResult> RenewAsync(
        IDatabase database,
        RedisKey key,
        RedisValue ownerToken,
        long leaseDurationMilliseconds)
        => database.ScriptEvaluateAsync(
            RenewScript,
            new[] { key },
            new RedisValue[] { ownerToken, leaseDurationMilliseconds });

    private sealed class RedisLockLease : IDistributedLockLease
    {
        private readonly IDatabase _database;
        private readonly RedisKey _key;
        private readonly RedisValue _ownerToken;
        private readonly long _leaseDurationMilliseconds;
        private readonly CancellationTokenSource _renewalCancellation = new();
        private readonly CancellationTokenSource _leaseLostCancellation = new();
        private readonly Task _renewalTask;
        private Exception? _renewalException;
        private int _released;

        public RedisLockLease(
            IDatabase database,
            RedisKey key,
            RedisValue ownerToken,
            TimeSpan leaseDuration)
        {
            _database = database;
            _key = key;
            _ownerToken = ownerToken;
            _leaseDurationMilliseconds = (long)Math.Ceiling(leaseDuration.TotalMilliseconds);
            LeaseLostToken = _leaseLostCancellation.Token;
            _renewalTask = RenewUntilReleasedAsync(leaseDuration);
        }

        public CancellationToken LeaseLostToken { get; }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                _renewalCancellation.Cancel();
                Exception? cleanupException = null;
                try
                {
                    await _renewalTask;
                }
                catch (Exception exception)
                {
                    cleanupException = exception;
                }

                try
                {
                    await ReleaseAsync(_database, _key, _ownerToken);
                }
                catch (Exception exception)
                {
                    cleanupException = cleanupException is null
                        ? exception
                        : new AggregateException(cleanupException, exception);
                }
                finally
                {
                    _renewalCancellation.Dispose();
                    _leaseLostCancellation.Dispose();
                }

                if (_renewalException is not null)
                {
                    cleanupException = cleanupException is null
                        ? _renewalException
                        : new AggregateException(cleanupException, _renewalException);
                }

                if (cleanupException is not null)
                {
                    throw new InvalidOperationException(
                        "The Redis lock lease could not be renewed or released.",
                        cleanupException);
                }
            }
        }

        private async Task RenewUntilReleasedAsync(TimeSpan leaseDuration)
        {
            var renewalInterval = TimeSpan.FromTicks(Math.Max(1, leaseDuration.Ticks / 3));

            try
            {
                while (true)
                {
                    await Task.Delay(renewalInterval, _renewalCancellation.Token);
                    var renewed = await RenewAsync(
                        _database,
                        _key,
                        _ownerToken,
                        _leaseDurationMilliseconds);
                    if ((long)renewed == 0)
                    {
                        SignalLeaseLost();
                        return;
                    }
                }
            }
            catch (OperationCanceledException) when (_renewalCancellation.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                _renewalException = exception;
                SignalLeaseLost();
            }
        }

        private void SignalLeaseLost()
        {
            try
            {
                _leaseLostCancellation.Cancel();
            }
            catch (AggregateException exception)
            {
                _renewalException ??= exception;
            }
        }
    }
}
