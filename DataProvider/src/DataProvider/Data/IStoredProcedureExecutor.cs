namespace DataProvider.Data;

/// <summary>
/// Dedicated abstraction for triggering stored procedures. Callers pass a
/// procedure name plus parameters and get results mapped to objects; the Dapper
/// details stay behind this seam.
/// </summary>
public interface IStoredProcedureExecutor
{
    Task<IReadOnlyList<T>> QueryAsync<T>(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    Task<T?> QuerySingleOrDefaultAsync<T>(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    Task<int> ExecuteAsync(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default);
}
