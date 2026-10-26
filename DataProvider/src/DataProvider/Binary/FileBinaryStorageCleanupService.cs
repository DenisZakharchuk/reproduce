namespace DataProvider.Binary;

public sealed class FileBinaryStorageCleanupService : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);
    private readonly FileBinaryStorage _storage;

    public FileBinaryStorageCleanupService(FileBinaryStorage storage)
    {
        _storage = storage;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CleanupInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await _storage.RemoveExpiredEntriesAsync(stoppingToken).ConfigureAwait(false);
        }
    }
}
