using FakeItEasy;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using WateringController.Backend.Hubs;
using WateringController.Backend.Mqtt;
using WateringController.Backend.State;
using Xunit;

namespace WateringController.Backend.Tests;

public sealed class MqttHandlerTests
{
    [Fact]
    public async Task WaterLevelHandler_UpdatesStore_AndBroadcasts()
    {
        var store = new WaterLevelStateStore();
        var (hubContext, proxy) = CreateHubContext();
        var logger = A.Fake<Microsoft.Extensions.Logging.ILogger<WaterLevelMqttHandler>>();

        var topics = new MqttTopics(Microsoft.Extensions.Options.Options.Create(new MqttOptions { TopicPrefix = "Test" }));
        var handler = new WaterLevelMqttHandler(store, topics, hubContext, logger);
        var payload = """
            {
              "levelPercent": 63,
              "sensors": [true, true, false, false],
              "measuredAt": "2026-02-01T10:00:00Z",
              "reportedAt": "2026-02-01T10:00:01Z"
            }
            """;

        await handler.HandleAsync(topics.WaterLevelState, System.Text.Encoding.UTF8.GetBytes(payload), true, DateTimeOffset.UtcNow);

        Assert.NotNull(store.GetLatest());
        A.CallTo(() => proxy.SendCoreAsync("WaterLevelUpdated", A<object?[]>.Ignored, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task WaterLevelHandler_AcceptsNonRetained()
    {
        var store = new WaterLevelStateStore();
        var (hubContext, proxy) = CreateHubContext();
        var logger = A.Fake<Microsoft.Extensions.Logging.ILogger<WaterLevelMqttHandler>>();

        var topics = new MqttTopics(Microsoft.Extensions.Options.Options.Create(new MqttOptions { TopicPrefix = "Test" }));
        var handler = new WaterLevelMqttHandler(store, topics, hubContext, logger);
        var payload = """
            {
              "levelPercent": 40,
              "sensors": [true, false, false, false],
              "measuredAt": "2026-02-01T10:10:00Z",
              "reportedAt": "2026-02-01T10:10:01Z"
            }
            """;

        await handler.HandleAsync(topics.WaterLevelState, System.Text.Encoding.UTF8.GetBytes(payload), false, DateTimeOffset.UtcNow);

        Assert.NotNull(store.GetLatest());
        A.CallTo(() => proxy.SendCoreAsync("WaterLevelUpdated", A<object?[]>.Ignored, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task WaterLevelHandler_RejectsInvalidSensorsLength()
    {
        var store = new WaterLevelStateStore();
        var (hubContext, proxy) = CreateHubContext();
        var logger = A.Fake<Microsoft.Extensions.Logging.ILogger<WaterLevelMqttHandler>>();

        var topics = new MqttTopics(Microsoft.Extensions.Options.Options.Create(new MqttOptions { TopicPrefix = "Test" }));
        var handler = new WaterLevelMqttHandler(store, topics, hubContext, logger);
        var payload = """
            {
              "levelPercent": 40,
              "sensors": [true, false, false],
              "measuredAt": "2026-02-01T10:10:00Z",
              "reportedAt": "2026-02-01T10:10:01Z"
            }
            """;

        await handler.HandleAsync(topics.WaterLevelState, System.Text.Encoding.UTF8.GetBytes(payload), true, DateTimeOffset.UtcNow);

        Assert.Null(store.GetLatest());
        A.CallTo(() => proxy.SendCoreAsync("WaterLevelUpdated", A<object?[]>.Ignored, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task WaterLevelHandler_RejectsNonObjectPayload()
    {
        var store = new WaterLevelStateStore();
        var (hubContext, proxy) = CreateHubContext();
        var logger = A.Fake<Microsoft.Extensions.Logging.ILogger<WaterLevelMqttHandler>>();

        var topics = new MqttTopics(Microsoft.Extensions.Options.Options.Create(new MqttOptions { TopicPrefix = "Test" }));
        var handler = new WaterLevelMqttHandler(store, topics, hubContext, logger);
        var payload = "\"not-an-object\"";

        await handler.HandleAsync(topics.WaterLevelState, System.Text.Encoding.UTF8.GetBytes(payload), true, DateTimeOffset.UtcNow);

        Assert.Null(store.GetLatest());
        A.CallTo(() => proxy.SendCoreAsync("WaterLevelUpdated", A<object?[]>.Ignored, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task WaterLevelHandler_RejectsMissingLevelPercent()
    {
        var store = new WaterLevelStateStore();
        var (hubContext, proxy) = CreateHubContext();
        var logger = A.Fake<Microsoft.Extensions.Logging.ILogger<WaterLevelMqttHandler>>();

        var topics = new MqttTopics(Microsoft.Extensions.Options.Options.Create(new MqttOptions { TopicPrefix = "Test" }));
        var handler = new WaterLevelMqttHandler(store, topics, hubContext, logger);
        var payload = """
            {
              "sensors": [true, false, false, false],
              "measuredAt": "2026-02-01T10:10:00Z",
              "reportedAt": "2026-02-01T10:10:01Z"
            }
            """;

        await handler.HandleAsync(topics.WaterLevelState, System.Text.Encoding.UTF8.GetBytes(payload), true, DateTimeOffset.UtcNow);

        Assert.Null(store.GetLatest());
        A.CallTo(() => proxy.SendCoreAsync("WaterLevelUpdated", A<object?[]>.Ignored, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task PumpStateHandler_AllowsSinceNullWhenNotRunning()
    {
        var store = new PumpStateStore();
        var (hubContext, proxy) = CreateHubContext();
        var logger = A.Fake<Microsoft.Extensions.Logging.ILogger<PumpStateMqttHandler>>();

        var topics = new MqttTopics(Microsoft.Extensions.Options.Options.Create(new MqttOptions { TopicPrefix = "Test" }));
        var handler = new PumpStateMqttHandler(store, topics, hubContext, logger);
        var payload = """
            {
              "running": false,
              "since": null,
              "lastRunSeconds": 30,
              "lastRequestId": "abc",
              "reportedAt": "2026-02-01T10:05:00Z"
            }
            """;

        await handler.HandleAsync(topics.PumpState, System.Text.Encoding.UTF8.GetBytes(payload), true, DateTimeOffset.UtcNow);

        Assert.NotNull(store.GetLatest());
        A.CallTo(() => proxy.SendCoreAsync("PumpStateUpdated", A<object?[]>.Ignored, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task PumpStateHandler_AcceptsNonRetained()
    {
        var store = new PumpStateStore();
        var (hubContext, proxy) = CreateHubContext();
        var logger = A.Fake<Microsoft.Extensions.Logging.ILogger<PumpStateMqttHandler>>();

        var topics = new MqttTopics(Microsoft.Extensions.Options.Options.Create(new MqttOptions { TopicPrefix = "Test" }));
        var handler = new PumpStateMqttHandler(store, topics, hubContext, logger);
        var payload = """
            {
              "running": false,
              "since": null,
              "lastRunSeconds": 12,
              "lastRequestId": "abc",
              "reportedAt": "2026-02-01T10:06:00Z"
            }
            """;

        await handler.HandleAsync(topics.PumpState, System.Text.Encoding.UTF8.GetBytes(payload), false, DateTimeOffset.UtcNow);

        Assert.NotNull(store.GetLatest());
        A.CallTo(() => proxy.SendCoreAsync("PumpStateUpdated", A<object?[]>.Ignored, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task PumpStateHandler_RejectsInvalidReportedAtOffset()
    {
        var store = new PumpStateStore();
        var (hubContext, proxy) = CreateHubContext();
        var logger = A.Fake<Microsoft.Extensions.Logging.ILogger<PumpStateMqttHandler>>();

        var topics = new MqttTopics(Microsoft.Extensions.Options.Options.Create(new MqttOptions { TopicPrefix = "Test" }));
        var handler = new PumpStateMqttHandler(store, topics, hubContext, logger);
        var payload = """
            {
              "running": false,
              "since": null,
              "lastRunSeconds": 30,
              "lastRequestId": "abc",
              "reportedAt": "2026-02-01T10:05:00+01:00"
            }
            """;

        await handler.HandleAsync(topics.PumpState, System.Text.Encoding.UTF8.GetBytes(payload), true, DateTimeOffset.UtcNow);

        Assert.Null(store.GetLatest());
        A.CallTo(() => proxy.SendCoreAsync("PumpStateUpdated", A<object?[]>.Ignored, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task PumpStateHandler_RejectsNonObjectPayload()
    {
        var store = new PumpStateStore();
        var (hubContext, proxy) = CreateHubContext();
        var logger = A.Fake<Microsoft.Extensions.Logging.ILogger<PumpStateMqttHandler>>();

        var topics = new MqttTopics(Microsoft.Extensions.Options.Options.Create(new MqttOptions { TopicPrefix = "Test" }));
        var handler = new PumpStateMqttHandler(store, topics, hubContext, logger);
        var payload = "false";

        await handler.HandleAsync(topics.PumpState, System.Text.Encoding.UTF8.GetBytes(payload), true, DateTimeOffset.UtcNow);

        Assert.Null(store.GetLatest());
        A.CallTo(() => proxy.SendCoreAsync("PumpStateUpdated", A<object?[]>.Ignored, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task PumpStateHandler_RejectsMissingRunning()
    {
        var store = new PumpStateStore();
        var (hubContext, proxy) = CreateHubContext();
        var logger = A.Fake<Microsoft.Extensions.Logging.ILogger<PumpStateMqttHandler>>();

        var topics = new MqttTopics(Microsoft.Extensions.Options.Options.Create(new MqttOptions { TopicPrefix = "Test" }));
        var handler = new PumpStateMqttHandler(store, topics, hubContext, logger);
        var payload = """
            {
              "since": null,
              "lastRunSeconds": 30,
              "reportedAt": "2026-02-01T10:05:00Z"
            }
            """;

        await handler.HandleAsync(topics.PumpState, System.Text.Encoding.UTF8.GetBytes(payload), true, DateTimeOffset.UtcNow);

        Assert.Null(store.GetLatest());
        A.CallTo(() => proxy.SendCoreAsync("PumpStateUpdated", A<object?[]>.Ignored, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task PumpStateHandler_RejectsRunningWithoutSince()
    {
        var store = new PumpStateStore();
        var (hubContext, proxy) = CreateHubContext();
        var logger = A.Fake<Microsoft.Extensions.Logging.ILogger<PumpStateMqttHandler>>();

        var topics = new MqttTopics(Microsoft.Extensions.Options.Options.Create(new MqttOptions { TopicPrefix = "Test" }));
        var handler = new PumpStateMqttHandler(store, topics, hubContext, logger);
        var payload = """
            {
              "running": true,
              "lastRunSeconds": 30,
              "reportedAt": "2026-02-01T10:05:00Z"
            }
            """;

        await handler.HandleAsync(topics.PumpState, System.Text.Encoding.UTF8.GetBytes(payload), true, DateTimeOffset.UtcNow);

        Assert.Null(store.GetLatest());
        A.CallTo(() => proxy.SendCoreAsync("PumpStateUpdated", A<object?[]>.Ignored, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task AlarmHandler_AddsAlarm_AndBroadcasts()
    {
        await using var scope = await CreateAlarmStoreAsync();
        var store = scope.Store;
        var (hubContext, proxy) = CreateHubContext();
        var logger = A.Fake<Microsoft.Extensions.Logging.ILogger<SystemAlarmMqttHandler>>();

        var topics = new MqttTopics(Microsoft.Extensions.Options.Options.Create(new MqttOptions { TopicPrefix = "Test" }));
        var handler = new SystemAlarmMqttHandler(store, topics, hubContext, logger);
        var payload = """
            {
              "type": "LOW_WATER",
              "severity": "warning",
              "message": "Pump run blocked due to low water level",
              "raisedAt": "2026-02-01T10:10:00Z"
            }
            """;

        await handler.HandleAsync(topics.SystemAlarm, System.Text.Encoding.UTF8.GetBytes(payload), false, DateTimeOffset.UtcNow);

        Assert.NotEmpty(store.GetRecent());
        A.CallTo(() => proxy.SendCoreAsync("AlarmRaised", A<object?[]>.Ignored, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task AlarmHandler_AcceptsRetained()
    {
        await using var scope = await CreateAlarmStoreAsync();
        var store = scope.Store;
        var (hubContext, proxy) = CreateHubContext();
        var logger = A.Fake<Microsoft.Extensions.Logging.ILogger<SystemAlarmMqttHandler>>();

        var topics = new MqttTopics(Microsoft.Extensions.Options.Options.Create(new MqttOptions { TopicPrefix = "Test" }));
        var handler = new SystemAlarmMqttHandler(store, topics, hubContext, logger);
        var payload = """
            {
              "type": "MQTT_DISCONNECTED",
              "severity": "warning",
              "message": "Pump run blocked due to MQTT disconnect",
              "raisedAt": "2026-02-01T10:12:00Z"
            }
            """;

        await handler.HandleAsync(topics.SystemAlarm, System.Text.Encoding.UTF8.GetBytes(payload), true, DateTimeOffset.UtcNow);

        Assert.NotEmpty(store.GetRecent());
        A.CallTo(() => proxy.SendCoreAsync("AlarmRaised", A<object?[]>.Ignored, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task AlarmHandler_RejectsMissingType()
    {
        await using var scope = await CreateAlarmStoreAsync();
        var store = scope.Store;
        var (hubContext, proxy) = CreateHubContext();
        var logger = A.Fake<Microsoft.Extensions.Logging.ILogger<SystemAlarmMqttHandler>>();

        var topics = new MqttTopics(Microsoft.Extensions.Options.Options.Create(new MqttOptions { TopicPrefix = "Test" }));
        var handler = new SystemAlarmMqttHandler(store, topics, hubContext, logger);
        var payload = """
            {
              "severity": "warning",
              "message": "Pump run blocked due to MQTT disconnect",
              "raisedAt": "2026-02-01T10:12:00Z"
            }
            """;

        await handler.HandleAsync(topics.SystemAlarm, System.Text.Encoding.UTF8.GetBytes(payload), true, DateTimeOffset.UtcNow);

        Assert.Empty(store.GetRecent());
        A.CallTo(() => proxy.SendCoreAsync("AlarmRaised", A<object?[]>.Ignored, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task AlarmHandler_RejectsNonObjectPayload()
    {
        await using var scope = await CreateAlarmStoreAsync();
        var store = scope.Store;
        var (hubContext, proxy) = CreateHubContext();
        var logger = A.Fake<Microsoft.Extensions.Logging.ILogger<SystemAlarmMqttHandler>>();

        var topics = new MqttTopics(Microsoft.Extensions.Options.Options.Create(new MqttOptions { TopicPrefix = "Test" }));
        var handler = new SystemAlarmMqttHandler(store, topics, hubContext, logger);
        var payload = "[]";

        await handler.HandleAsync(topics.SystemAlarm, System.Text.Encoding.UTF8.GetBytes(payload), true, DateTimeOffset.UtcNow);

        Assert.Empty(store.GetRecent());
        A.CallTo(() => proxy.SendCoreAsync("AlarmRaised", A<object?[]>.Ignored, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task AlarmHandler_RejectsMissingRaisedAt()
    {
        await using var scope = await CreateAlarmStoreAsync();
        var store = scope.Store;
        var (hubContext, proxy) = CreateHubContext();
        var logger = A.Fake<Microsoft.Extensions.Logging.ILogger<SystemAlarmMqttHandler>>();

        var topics = new MqttTopics(Microsoft.Extensions.Options.Options.Create(new MqttOptions { TopicPrefix = "Test" }));
        var handler = new SystemAlarmMqttHandler(store, topics, hubContext, logger);
        var payload = """
            {
              "type": "LOW_WATER",
              "severity": "warning",
              "message": "Pump run blocked due to low water level"
            }
            """;

        await handler.HandleAsync(topics.SystemAlarm, System.Text.Encoding.UTF8.GetBytes(payload), true, DateTimeOffset.UtcNow);

        Assert.Empty(store.GetRecent());
        A.CallTo(() => proxy.SendCoreAsync("AlarmRaised", A<object?[]>.Ignored, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    private static (IHubContext<WateringHub> hubContext, IClientProxy proxy) CreateHubContext()
    {
        var hubContext = A.Fake<IHubContext<WateringHub>>();
        var clients = A.Fake<IHubClients>();
        var proxy = A.Fake<IClientProxy>();

        A.CallTo(() => clients.All).Returns(proxy);
        A.CallTo(() => hubContext.Clients).Returns(clients);

        return (hubContext, proxy);
    }

    private static async Task<AlarmStoreScope> CreateAlarmStoreAsync()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"watering-alarms-{Guid.NewGuid():N}.db");
        var options = Microsoft.Extensions.Options.Options.Create(new WateringController.Backend.Options.DatabaseOptions
        {
            ConnectionString = $"Data Source={dbPath}"
        });
        var factory = new WateringController.Backend.Data.SqliteConnectionFactory(options);
        var repository = new WateringController.Backend.Data.AlarmRepository(factory);
        var initializer = new WateringController.Backend.Data.DbInitializer(factory, A.Fake<Microsoft.Extensions.Logging.ILogger<WateringController.Backend.Data.DbInitializer>>());
        await initializer.StartAsync(CancellationToken.None);

        return new AlarmStoreScope(new AlarmStore(repository), dbPath);
    }

    private sealed class AlarmStoreScope : IAsyncDisposable
    {
        private readonly string _dbPath;
        public AlarmStore Store { get; }

        public AlarmStoreScope(AlarmStore store, string dbPath)
        {
            Store = store;
            _dbPath = dbPath;
        }

        public ValueTask DisposeAsync()
        {
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
