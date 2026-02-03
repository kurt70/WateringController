using Microsoft.Data.Sqlite;
using WateringController.Backend.Models;

namespace WateringController.Backend.Data;

/// <summary>
/// Provides persistence for alarm records.
/// </summary>
public sealed class AlarmRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public AlarmRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(AlarmRecord alarm, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO alarms (type, severity, message, raised_at_utc, received_at_utc)
            VALUES ($type, $severity, $message, $raised_at_utc, $received_at_utc);
            """;
        command.Parameters.AddWithValue("$type", alarm.Type);
        command.Parameters.AddWithValue("$severity", alarm.Severity);
        command.Parameters.AddWithValue("$message", alarm.Message);
        command.Parameters.AddWithValue("$raised_at_utc", alarm.RaisedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$received_at_utc", alarm.ReceivedAtUtc.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AlarmRecord>> GetRecentAsync(int limit, CancellationToken cancellationToken)
    {
        var results = new List<AlarmRecord>();
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, type, severity, message, raised_at_utc, received_at_utc
            FROM alarms
            ORDER BY id DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$limit", limit);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new AlarmRecord
            {
                Id = reader.GetInt32(0),
                Type = reader.GetString(1),
                Severity = reader.GetString(2),
                Message = reader.GetString(3),
                RaisedAtUtc = DateTimeOffset.Parse(reader.GetString(4)),
                ReceivedAtUtc = DateTimeOffset.Parse(reader.GetString(5))
            });
        }

        return results;
    }
}
