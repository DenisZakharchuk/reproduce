using System.Data;
using DataProvider.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace DataProvider.Data;

public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly IOptionsMonitor<DatabaseOptions> _options;

    public SqlConnectionFactory(IOptionsMonitor<DatabaseOptions> options)
    {
        _options = options;
    }

    public IDbConnection CreateConnection()
        => new SqlConnection(_options.CurrentValue.ConnectionString);
}
