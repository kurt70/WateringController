using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using WateringController.Backend.Options;

namespace WateringController.Backend.Data;

/// <summary>
/// Creates SQLite connections based on configured connection strings.
/// </summary>
public sealed class SqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(IOptions<DatabaseOptions> options)
    {
        _connectionString = options.Value.ConnectionString;
    }

    public SqliteConnection Create()
    {
        return new SqliteConnection(_connectionString);
    }
}
