using Microsoft.Data.Sqlite;
using WateringController.Backend.Models;

namespace WateringController.Backend.Data;

/// <summary>
/// Provides persistence for pump run history entries.
/// </summary>
public sealed class RunHistoryRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public RunHistoryRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(RunHistoryEntry entry, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO run_history (schedule_id, requested_at_utc, run_seconds, allowed, reason)
            VALUES ($schedule_id, $requested_at_utc, $run_seconds, $allowed, $reason);
            """;
        command.Parameters.AddWithValue("$schedule_id", (object?)entry.ScheduleId ?? DBNull.Value);
        command.Parameters.AddWithValue("$requested_at_utc", entry.RequestedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$run_seconds", entry.RunSeconds);
        command.Parameters.AddWithValue("$allowed", entry.Allowed ? 1 : 0);
        command.Parameters.AddWithValue("$reason", entry.Reason);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RunHistoryEntry>> GetRecentAsync(int limit, CancellationToken cancellationToken)
    {
        var results = new List<RunHistoryEntry>();
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, schedule_id, requested_at_utc, run_seconds, allowed, reason
            FROM run_history
            ORDER BY id DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$limit", limit);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new RunHistoryEntry
            {
                Id = reader.GetInt32(0),
                ScheduleId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                RequestedAtUtc = DateTimeOffset.Parse(reader.GetString(2)),
                RunSeconds = reader.GetInt32(3),
                Allowed = reader.GetInt32(4) == 1,
                Reason = reader.GetString(5)
            });
        }

        return results;
    }
}
