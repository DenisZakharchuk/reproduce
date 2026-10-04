using System.Data;
using Dapper;

namespace DataProvider.Data;

public class DapperStoredProcedureExecutor : IStoredProcedureExecutor
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DapperStoredProcedureExecutor(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<T>> QueryAsync<T>(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var command = CreateCommand(procedureName, parameters, cancellationToken);
        var result = await connection.QueryAsync<T>(command);
        return result.AsList();
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var command = CreateCommand(procedureName, parameters, cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<T>(command);
    }

    public async Task<int> ExecuteAsync(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var command = CreateCommand(procedureName, parameters, cancellationToken);
        return await connection.ExecuteAsync(command);
    }

    private static CommandDefinition CreateCommand(
        string procedureName,
        object? parameters,
        CancellationToken cancellationToken)
        => new(
            procedureName,
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
}
