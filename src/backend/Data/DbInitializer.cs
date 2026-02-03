using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WateringController.Backend.Data;

/// <summary>
/// Ensures required SQLite tables exist on startup.
/// </summary>
public sealed class DbInitializer : IHostedService
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(SqliteConnectionFactory connectionFactory, ILogger<DbInitializer> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <summary>
    /// Creates schema if missing. Safe to call multiple times.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = _connectionFactory.Create();
            await connection.OpenAsync(cancellationToken);

            var commands = new[]
            {
                """
                CREATE TABLE IF NOT EXISTS schedules (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    enabled INTEGER NOT NULL,
                    start_time_utc TEXT NOT NULL,
                    run_seconds INTEGER NOT NULL,
                    days_of_week TEXT NULL,
                    last_run_date_utc TEXT NULL
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS run_history (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    schedule_id INTEGER NULL,
                    requested_at_utc TEXT NOT NULL,
                    run_seconds INTEGER NOT NULL,
                    allowed INTEGER NOT NULL,
                    reason TEXT NOT NULL
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS alarms (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    type TEXT NOT NULL,
                    severity TEXT NOT NULL,
                    message TEXT NOT NULL,
                    raised_at_utc TEXT NOT NULL,
                    received_at_utc TEXT NOT NULL
                );
                """
            };

            foreach (var sql in commands)
            {
                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            _logger.LogInformation("SQLite schema ensured.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
