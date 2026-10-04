using System.Data;

namespace DataProvider.Data;

/// <summary>
/// Creates open-able database connections. Isolates connection creation so the
/// rest of the app never depends on a concrete ADO.NET provider.
/// </summary>
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
