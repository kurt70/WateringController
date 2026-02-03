using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using WateringController.Backend.Contracts;
using WateringController.Backend.Data;
using WateringController.Backend.Mqtt;
using WateringController.Backend.Models;
using WateringController.Backend.Services;
using WateringController.Backend.State;
using Xunit;

namespace WateringController.Backend.Tests;

public sealed class ScheduleExecutionTests
{
    [Fact]
    public async Task EvaluateSchedules_PublishesWhenDueAndSafe()
    {
        var nowUtc = new DateTimeOffset(2026, 2, 2, 7, 0, 10, TimeSpan.Zero);
        var publisher = A.Fake<IMqttPublisher>();
        A.CallTo(() => publisher.IsConnected).Returns(true);

        await using var scope = await CreateScopeWithDbAsync(publisher, nowUtc);
        var provider = scope.ServiceProvider;

        var scheduleRepo = provider.GetRequiredService<ScheduleRepository>();
        await scheduleRepo.AddAsync(new WateringSchedule
        {
            Enabled = true,
            StartTimeUtc = "07:00",
            RunSeconds = 20,
            DaysOfWeek = "Mon"
        }, CancellationToken.None);

        var waterStore = provider.GetRequiredService<WaterLevelStateStore>();
        waterStore.Update(new WaterLevelStatePayload
        {
            LevelPercent = 50,
            Sensors = new[] { true, true, true, true },
            MeasuredAt = nowUtc,
            ReportedAt = nowUtc
        }, nowUtc);

        var service = provider.GetRequiredService<ScheduleService>();
        await InvokeEvaluateSchedulesAsync(service);

        var topics = provider.GetRequiredService<MqttTopics>();
        A.CallTo(() => publisher.PublishAsync(topics.PumpCommand, A<string>._, false, A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Fact]
    public async Task EvaluateSchedules_RecordsHistoryWhenBlocked()
    {
        var nowUtc = new DateTimeOffset(2026, 2, 2, 7, 0, 5, TimeSpan.Zero);
        var publisher = A.Fake<IMqttPublisher>();
        A.CallTo(() => publisher.IsConnected).Returns(true);

        await using var scope = await CreateScopeWithDbAsync(publisher, nowUtc);
        var provider = scope.ServiceProvider;

        var scheduleRepo = provider.GetRequiredService<ScheduleRepository>();
        await scheduleRepo.AddAsync(new WateringSchedule
        {
            Enabled = true,
            StartTimeUtc = "07:00",
            RunSeconds = 20,
            DaysOfWeek = "Mon"
        }, CancellationToken.None);

        var service = provider.GetRequiredService<ScheduleService>();
        await InvokeEvaluateSchedulesAsync(service);

        var historyRepo = provider.GetRequiredService<RunHistoryRepository>();
        var history = await historyRepo.GetRecentAsync(10, CancellationToken.None);

        Assert.NotEmpty(history);
        Assert.Contains(history, entry => entry.Allowed == false);
    }

    [Fact]
    public async Task EvaluateSchedules_RaisesAlarmWhenLevelEmpty()
    {
        var nowUtc = new DateTimeOffset(2026, 2, 2, 7, 0, 5, TimeSpan.Zero);
        var publisher = A.Fake<IMqttPublisher>();
        A.CallTo(() => publisher.IsConnected).Returns(true);

        await using var scope = await CreateScopeWithDbAsync(publisher, nowUtc);
        var provider = scope.ServiceProvider;

        var scheduleRepo = provider.GetRequiredService<ScheduleRepository>();
        await scheduleRepo.AddAsync(new WateringSchedule
        {
            Enabled = true,
            StartTimeUtc = "07:00",
            RunSeconds = 20,
            DaysOfWeek = "Mon"
        }, CancellationToken.None);

        var waterStore = provider.GetRequiredService<WaterLevelStateStore>();
        waterStore.Update(new WaterLevelStatePayload
        {
            LevelPercent = 0,
            Sensors = new[] { false, false, false, false },
            MeasuredAt = nowUtc,
            ReportedAt = nowUtc
        }, nowUtc);

        var service = provider.GetRequiredService<ScheduleService>();
        await InvokeEvaluateSchedulesAsync(service);

        var alarmStore = provider.GetRequiredService<AlarmStore>();
        var alarms = alarmStore.GetRecent();
        Assert.Contains(alarms, alarm => alarm.Type == "LOW_WATER");
    }

    [Fact]
    public async Task EvaluateSchedules_RaisesAlarmWhenLevelStale()
    {
        var nowUtc = new DateTimeOffset(2026, 2, 2, 7, 0, 5, TimeSpan.Zero);
        var publisher = A.Fake<IMqttPublisher>();
        A.CallTo(() => publisher.IsConnected).Returns(true);

        await using var scope = await CreateScopeWithDbAsync(publisher, nowUtc, staleMinutes: 1);
        var provider = scope.ServiceProvider;

        var scheduleRepo = provider.GetRequiredService<ScheduleRepository>();
        await scheduleRepo.AddAsync(new WateringSchedule
        {
            Enabled = true,
            StartTimeUtc = "07:00",
            RunSeconds = 20,
            DaysOfWeek = "Mon"
        }, CancellationToken.None);

        var waterStore = provider.GetRequiredService<WaterLevelStateStore>();
        var staleTime = nowUtc.AddMinutes(-5);
        waterStore.Update(new WaterLevelStatePayload
        {
            LevelPercent = 20,
            Sensors = new[] { true, true, false, false },
            MeasuredAt = staleTime,
            ReportedAt = staleTime
        }, staleTime);

        var service = provider.GetRequiredService<ScheduleService>();
        await InvokeEvaluateSchedulesAsync(service);

        var alarmStore = provider.GetRequiredService<AlarmStore>();
        var alarms = alarmStore.GetRecent();
        Assert.Contains(alarms, alarm => alarm.Type == "LEVEL_UNKNOWN");
    }

    [Fact]
    public async Task EvaluateSchedules_RaisesAlarmWhenMqttDisconnected()
    {
        var nowUtc = new DateTimeOffset(2026, 2, 2, 7, 0, 5, TimeSpan.Zero);
        var publisher = A.Fake<IMqttPublisher>();
        A.CallTo(() => publisher.IsConnected).Returns(false);

        await using var scope = await CreateScopeWithDbAsync(publisher, nowUtc);
        var provider = scope.ServiceProvider;

        var scheduleRepo = provider.GetRequiredService<ScheduleRepository>();
        await scheduleRepo.AddAsync(new WateringSchedule
        {
            Enabled = true,
            StartTimeUtc = "07:00",
            RunSeconds = 20,
            DaysOfWeek = "Mon"
        }, CancellationToken.None);

        var service = provider.GetRequiredService<ScheduleService>();
        await InvokeEvaluateSchedulesAsync(service);

        var alarmStore = provider.GetRequiredService<AlarmStore>();
        var alarms = alarmStore.GetRecent();
        Assert.Contains(alarms, alarm => alarm.Type == "MQTT_DISCONNECTED");
    }

    [Fact]
    public async Task EvaluateSchedules_UpdatesLastRunDate()
    {
        var nowUtc = new DateTimeOffset(2026, 2, 2, 7, 0, 10, TimeSpan.Zero);
        var publisher = A.Fake<IMqttPublisher>();
        A.CallTo(() => publisher.IsConnected).Returns(true);

        await using var scope = await CreateScopeWithDbAsync(publisher, nowUtc);
        var provider = scope.ServiceProvider;

        var scheduleRepo = provider.GetRequiredService<ScheduleRepository>();
        await scheduleRepo.AddAsync(new WateringSchedule
        {
            Enabled = true,
            StartTimeUtc = "07:00",
            RunSeconds = 20,
            DaysOfWeek = "Mon"
        }, CancellationToken.None);

        var waterStore = provider.GetRequiredService<WaterLevelStateStore>();
        waterStore.Update(new WaterLevelStatePayload
        {
            LevelPercent = 50,
            Sensors = new[] { true, true, true, true },
            MeasuredAt = nowUtc,
            ReportedAt = nowUtc
        }, nowUtc);

        var service = provider.GetRequiredService<ScheduleService>();
        await InvokeEvaluateSchedulesAsync(service);

        var schedules = await scheduleRepo.GetAllAsync(CancellationToken.None);
        var schedule = Assert.Single(schedules);
        Assert.Equal(nowUtc.ToString("yyyy-MM-dd"), schedule.LastRunDateUtc);
    }

    [Fact]
    public async Task EvaluateSchedules_SkipsWhenDayNotAllowed()
    {
        var nowUtc = new DateTimeOffset(2026, 2, 2, 7, 0, 10, TimeSpan.Zero);
        var publisher = A.Fake<IMqttPublisher>();
        A.CallTo(() => publisher.IsConnected).Returns(true);

        await using var scope = await CreateScopeWithDbAsync(publisher, nowUtc);
        var provider = scope.ServiceProvider;

        var scheduleRepo = provider.GetRequiredService<ScheduleRepository>();
        await scheduleRepo.AddAsync(new WateringSchedule
        {
            Enabled = true,
            StartTimeUtc = "07:00",
            RunSeconds = 20,
            DaysOfWeek = "Tue"
        }, CancellationToken.None);

        var waterStore = provider.GetRequiredService<WaterLevelStateStore>();
        waterStore.Update(new WaterLevelStatePayload
        {
            LevelPercent = 50,
            Sensors = new[] { true, true, true, true },
            MeasuredAt = nowUtc,
            ReportedAt = nowUtc
        }, nowUtc);

        var service = provider.GetRequiredService<ScheduleService>();
        await InvokeEvaluateSchedulesAsync(service);

        var topics = provider.GetRequiredService<MqttTopics>();
        A.CallTo(() => publisher.PublishAsync(topics.PumpCommand, A<string>._, false, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task EvaluateSchedules_SkipsWhenAlreadyRanToday()
    {
        var nowUtc = new DateTimeOffset(2026, 2, 2, 7, 0, 10, TimeSpan.Zero);
        var publisher = A.Fake<IMqttPublisher>();
        A.CallTo(() => publisher.IsConnected).Returns(true);

        await using var scope = await CreateScopeWithDbAsync(publisher, nowUtc);
        var provider = scope.ServiceProvider;

        var scheduleRepo = provider.GetRequiredService<ScheduleRepository>();
        await scheduleRepo.AddAsync(new WateringSchedule
        {
            Enabled = true,
            StartTimeUtc = "07:00",
            RunSeconds = 20,
            DaysOfWeek = "Mon",
            LastRunDateUtc = nowUtc.ToString("yyyy-MM-dd")
        }, CancellationToken.None);

        var waterStore = provider.GetRequiredService<WaterLevelStateStore>();
        waterStore.Update(new WaterLevelStatePayload
        {
            LevelPercent = 50,
            Sensors = new[] { true, true, true, true },
            MeasuredAt = nowUtc,
            ReportedAt = nowUtc
        }, nowUtc);

        var service = provider.GetRequiredService<ScheduleService>();
        await InvokeEvaluateSchedulesAsync(service);

        var topics = provider.GetRequiredService<MqttTopics>();
        A.CallTo(() => publisher.PublishAsync(topics.PumpCommand, A<string>._, false, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    private static async Task InvokeEvaluateSchedulesAsync(ScheduleService service)
    {
        var method = typeof(ScheduleService).GetMethod("EvaluateSchedulesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);

        var task = (Task)method!.Invoke(service, new object[] { CancellationToken.None })!;
        await task;
    }

    private static async Task<TestDbScope> CreateScopeWithDbAsync(
        IMqttPublisher publisher,
        DateTimeOffset nowUtc,
        int? staleMinutes = null)
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"watering-schedule-{Guid.NewGuid():N}.db");
        var builder = new TestServiceProviderBuilder()
            .WithSetting("Database:ConnectionString", $"Data Source={dbPath}")
            .WithSetting("Scheduling:CheckIntervalSeconds", "60")
            .WithMqttPublisher(publisher)
            .WithTimeProvider(new FixedTimeProvider(nowUtc));

        if (staleMinutes is not null)
        {
            builder.WithSetting("Safety:WaterLevelStaleMinutes", staleMinutes.Value.ToString());
        }
        else
        {
            builder.WithSetting("Safety:WaterLevelStaleMinutes", "60");
        }

        var provider = builder.Build();

        var initializer = provider.GetRequiredService<DbInitializer>();
        await initializer.StartAsync(CancellationToken.None);

        return new TestDbScope(provider, dbPath);
    }

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
                }
            }

            return ValueTask.CompletedTask;
        }
    }

}
