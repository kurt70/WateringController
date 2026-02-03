using Microsoft.Data.Sqlite;
using WateringController.Backend.Models;

namespace WateringController.Backend.Data;

/// <summary>
/// Provides persistence for watering schedules.
/// </summary>
public sealed class ScheduleRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public ScheduleRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<WateringSchedule>> GetAllAsync(CancellationToken cancellationToken)
    {
        var results = new List<WateringSchedule>();
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, enabled, start_time_utc, run_seconds, days_of_week, last_run_date_utc
            FROM schedules
            ORDER BY id;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new WateringSchedule
            {
                Id = reader.GetInt32(0),
                Enabled = reader.GetInt32(1) == 1,
                StartTimeUtc = reader.GetString(2),
                RunSeconds = reader.GetInt32(3),
                DaysOfWeek = reader.IsDBNull(4) ? null : reader.GetString(4),
                LastRunDateUtc = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }

        return results;
    }

    public async Task<int> AddAsync(WateringSchedule schedule, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO schedules (enabled, start_time_utc, run_seconds, days_of_week, last_run_date_utc)
            VALUES ($enabled, $start_time_utc, $run_seconds, $days_of_week, $last_run_date_utc);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$enabled", schedule.Enabled ? 1 : 0);
        command.Parameters.AddWithValue("$start_time_utc", schedule.StartTimeUtc);
        command.Parameters.AddWithValue("$run_seconds", schedule.RunSeconds);
        command.Parameters.AddWithValue("$days_of_week", (object?)schedule.DaysOfWeek ?? DBNull.Value);
        command.Parameters.AddWithValue("$last_run_date_utc", (object?)schedule.LastRunDateUtc ?? DBNull.Value);

        var id = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(id);
    }

    public async Task UpdateAsync(WateringSchedule schedule, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE schedules
            SET enabled = $enabled,
                start_time_utc = $start_time_utc,
                run_seconds = $run_seconds,
                days_of_week = $days_of_week
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$enabled", schedule.Enabled ? 1 : 0);
        command.Parameters.AddWithValue("$start_time_utc", schedule.StartTimeUtc);
        command.Parameters.AddWithValue("$run_seconds", schedule.RunSeconds);
        command.Parameters.AddWithValue("$days_of_week", (object?)schedule.DaysOfWeek ?? DBNull.Value);
        command.Parameters.AddWithValue("$id", schedule.Id);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateLastRunDateAsync(int scheduleId, string lastRunDateUtc, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE schedules
            SET last_run_date_utc = $last_run_date_utc
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$last_run_date_utc", lastRunDateUtc);
        command.Parameters.AddWithValue("$id", scheduleId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ClearLastRunDateAsync(int scheduleId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE schedules
            SET last_run_date_utc = NULL
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", scheduleId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM schedules
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
