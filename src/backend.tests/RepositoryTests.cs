using Microsoft.Extensions.DependencyInjection;
using WateringController.Backend.Data;
using WateringController.Backend.Models;
using Xunit;

namespace WateringController.Backend.Tests;

/// <summary>
/// Validates repository persistence against the SQLite backing store.
/// </summary>
public sealed class RepositoryTests
{
    [Fact]
    public async Task ScheduleRepository_AddsAndReads()
    {
        await using var scope = await CreateScopeWithDbAsync();
        var repository = scope.ServiceProvider.GetRequiredService<ScheduleRepository>();

        var id = await repository.AddAsync(new WateringSchedule
        {
            Enabled = true,
            StartTimeUtc = "07:00",
            RunSeconds = 30,
            DaysOfWeek = "Mon,Wed"
        }, CancellationToken.None);

        var schedules = await repository.GetAllAsync(CancellationToken.None);
        Assert.Contains(schedules, schedule => schedule.Id == id);
    }

    [Fact]
    public async Task ScheduleRepository_UpdatesLastRunDate()
    {
        await using var scope = await CreateScopeWithDbAsync();
        var repository = scope.ServiceProvider.GetRequiredService<ScheduleRepository>();

        var id = await repository.AddAsync(new WateringSchedule
        {
            Enabled = true,
            StartTimeUtc = "07:00",
            RunSeconds = 30,
            DaysOfWeek = "Mon"
        }, CancellationToken.None);

        await repository.UpdateLastRunDateAsync(id, "2026-02-02", CancellationToken.None);

        var schedules = await repository.GetAllAsync(CancellationToken.None);
        var schedule = Assert.Single(schedules, item => item.Id == id);
        Assert.Equal("2026-02-02", schedule.LastRunDateUtc);
    }

    [Fact]
    public async Task ScheduleRepository_ClearsLastRunDate()
    {
        await using var scope = await CreateScopeWithDbAsync();
        var repository = scope.ServiceProvider.GetRequiredService<ScheduleRepository>();

        var id = await repository.AddAsync(new WateringSchedule
        {
            Enabled = true,
            StartTimeUtc = "07:00",
            RunSeconds = 30,
            DaysOfWeek = "Mon",
            LastRunDateUtc = "2026-02-02"
        }, CancellationToken.None);

        await repository.ClearLastRunDateAsync(id, CancellationToken.None);

        var schedules = await repository.GetAllAsync(CancellationToken.None);
        var schedule = Assert.Single(schedules, item => item.Id == id);
        Assert.Null(schedule.LastRunDateUtc);
    }

    [Fact]
    public async Task ScheduleRepository_DeletesSchedule()
    {
        await using var scope = await CreateScopeWithDbAsync();
        var repository = scope.ServiceProvider.GetRequiredService<ScheduleRepository>();

        var id = await repository.AddAsync(new WateringSchedule
        {
            Enabled = true,
            StartTimeUtc = "07:00",
            RunSeconds = 30,
            DaysOfWeek = "Mon"
        }, CancellationToken.None);

        await repository.DeleteAsync(id, CancellationToken.None);

        var schedules = await repository.GetAllAsync(CancellationToken.None);
        Assert.DoesNotContain(schedules, schedule => schedule.Id == id);
    }

    [Fact]
    public async Task RunHistoryRepository_AddsAndReads()
    {
        await using var scope = await CreateScopeWithDbAsync();
        var repository = scope.ServiceProvider.GetRequiredService<RunHistoryRepository>();

        await repository.AddAsync(new RunHistoryEntry
        {
            ScheduleId = null,
            RequestedAtUtc = DateTimeOffset.UtcNow,
            RunSeconds = 25,
            Allowed = true,
            Reason = "schedule"
        }, CancellationToken.None);

        var history = await repository.GetRecentAsync(10, CancellationToken.None);
        Assert.NotEmpty(history);
    }

    [Fact]
    public async Task AlarmRepository_AddsAndReads()
    {
        await using var scope = await CreateScopeWithDbAsync();
        var repository = scope.ServiceProvider.GetRequiredService<AlarmRepository>();

        await repository.AddAsync(new AlarmRecord
        {
            Type = "LOW_WATER",
            Severity = "warning",
            Message = "Test alarm",
            RaisedAtUtc = DateTimeOffset.UtcNow,
            ReceivedAtUtc = DateTimeOffset.UtcNow
        }, CancellationToken.None);

        var alarms = await repository.GetRecentAsync(10, CancellationToken.None);
        Assert.NotEmpty(alarms);
    }

    private static async Task<TestDbScope> CreateScopeWithDbAsync()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"watering-tests-{Guid.NewGuid():N}.db");
        var provider = new TestServiceProviderBuilder()
            .WithSetting("Database:ConnectionString", $"Data Source={dbPath}")
            .Build();

        var initializer = provider.GetRequiredService<DbInitializer>();
        await initializer.StartAsync(CancellationToken.None);

        return new TestDbScope(provider, dbPath);
    }

    /// <summary>
    /// Owns the temporary test database and cleans it up.
    /// </summary>
    private sealed class TestDbScope : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly string _dbPath;

        public TestDbScope(ServiceProvider provider, string dbPath)
        {
            _provider = provider;
            _dbPath = dbPath;
        }

        public IServiceProvider ServiceProvider => _provider;

        public ValueTask DisposeAsync()
        {
            _provider.Dispose();
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(_dbPath))
            {
                try
                {
                    File.Delete(_dbPath);
                }
                catch (IOException)
                {
                    // Ignore file locks on Windows test cleanup.
                }
            }

            return ValueTask.CompletedTask;
        }
    }
}
