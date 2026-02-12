using System.Net;
using System.Net.Http.Json;
using FakeItEasy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using WateringController.Backend.Contracts;
using WateringController.Backend.Data;
using WateringController.Backend.Mqtt;
using WateringController.Backend.Models;
using WateringController.Backend.State;
using Xunit;

namespace WateringController.Backend.Tests;

/// <summary>
/// Covers HTTP endpoints used by the backend.
/// </summary>
public sealed class ApiEndpointsTests
{
    [Fact]
    public async Task ScheduleEndpoints_CreateAndList()
    {
        using var factory = new TestFactory();
        var client = factory.CreateClient();

        var request = new ScheduleRequest
        {
            Enabled = true,
            StartTimeUtc = "07:00",
            RunSeconds = 30,
            DaysOfWeek = "Mon,Wed"
        };

        var response = await client.PostAsJsonAsync("/api/schedules", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list = await client.GetFromJsonAsync<List<WateringSchedule>>("/api/schedules");
        Assert.NotNull(list);
        Assert.NotEmpty(list);
    }

    [Fact]
    public async Task ScheduleEndpoints_RejectInvalidRunSeconds()
    {
        using var factory = new TestFactory();
        var client = factory.CreateClient();

        var request = new ScheduleRequest
        {
            Enabled = true,
            StartTimeUtc = "07:00",
            RunSeconds = 0,
            DaysOfWeek = "Mon"
        };

        var response = await client.PostAsJsonAsync("/api/schedules", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var message = await response.Content.ReadAsStringAsync();
        Assert.Contains("runSeconds", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScheduleEndpoints_RejectInvalidTime()
    {
        using var factory = new TestFactory();
        var client = factory.CreateClient();

        var request = new ScheduleRequest
        {
            Enabled = true,
            StartTimeUtc = "25:90",
            RunSeconds = 10,
            DaysOfWeek = "Mon"
        };

        var response = await client.PostAsJsonAsync("/api/schedules", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var message = await response.Content.ReadAsStringAsync();
        Assert.Contains("startTimeUtc", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScheduleEndpoints_UpdateExisting()
    {
        using var factory = new TestFactory();
        var client = factory.CreateClient();

        var create = new ScheduleRequest
        {
            Enabled = true,
            StartTimeUtc = "07:00",
            RunSeconds = 30,
            DaysOfWeek = "Mon,Wed"
        };

        var createResponse = await client.PostAsJsonAsync("/api/schedules", create);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<IdResponse>();
        Assert.NotNull(created);

        var update = new ScheduleRequest
        {
            Enabled = true,
            StartTimeUtc = "07:00",
            RunSeconds = 45,
            DaysOfWeek = "Mon,Wed"
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/schedules/{created!.Id}", update);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var list = await client.GetFromJsonAsync<List<WateringSchedule>>("/api/schedules");
        Assert.NotNull(list);
        var updated = Assert.Single(list!, item => item.Id == created.Id);
        Assert.Equal(45, updated.RunSeconds);
    }

    [Fact]
    public async Task ScheduleEndpoints_UpdateRejectsInvalidRunSeconds()
    {
        using var factory = new TestFactory();
        var client = factory.CreateClient();

        var create = new ScheduleRequest
        {
            Enabled = true,
            StartTimeUtc = "07:00",
            RunSeconds = 30,
            DaysOfWeek = "Mon,Wed"
        };

        var createResponse = await client.PostAsJsonAsync("/api/schedules", create);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<IdResponse>();
        Assert.NotNull(created);

        var update = new ScheduleRequest
        {
            Enabled = true,
            StartTimeUtc = "07:00",
            RunSeconds = 0,
            DaysOfWeek = "Mon,Wed"
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/schedules/{created!.Id}", update);
        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);

        var message = await updateResponse.Content.ReadAsStringAsync();
        Assert.Contains("runSeconds", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScheduleEndpoints_UpdateRejectsInvalidTime()
    {
        using var factory = new TestFactory();
        var client = factory.CreateClient();

        var create = new ScheduleRequest
        {
            Enabled = true,
            StartTimeUtc = "07:00",
            RunSeconds = 30,
            DaysOfWeek = "Mon,Wed"
        };

        var createResponse = await client.PostAsJsonAsync("/api/schedules", create);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<IdResponse>();
        Assert.NotNull(created);

        var update = new ScheduleRequest
        {
            Enabled = true,
            StartTimeUtc = "25:90",
            RunSeconds = 20,
            DaysOfWeek = "Mon,Wed"
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/schedules/{created!.Id}", update);
        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);

        var message = await updateResponse.Content.ReadAsStringAsync();
        Assert.Contains("startTimeUtc", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScheduleEndpoints_UpdateEnabledAndDaysOfWeek()
    {
        using var factory = new TestFactory();
        var client = factory.CreateClient();

        var create = new ScheduleRequest
        {
            Enabled = true,
            StartTimeUtc = "07:00",
            RunSeconds = 30,
            DaysOfWeek = "Mon,Wed"
        };

        var createResponse = await client.PostAsJsonAsync("/api/schedules", create);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<IdResponse>();
        Assert.NotNull(created);

        var update = new ScheduleRequest
        {
            Enabled = false,
            StartTimeUtc = "07:00",
            RunSeconds = 30,
            DaysOfWeek = "Tue,Thu"
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/schedules/{created!.Id}", update);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var list = await client.GetFromJsonAsync<List<WateringSchedule>>("/api/schedules");
        Assert.NotNull(list);
        var updated = Assert.Single(list!, item => item.Id == created.Id);
        Assert.False(updated.Enabled);
        Assert.Equal("Tue,Thu", updated.DaysOfWeek);
    }

    [Fact]
    public async Task ScheduleEndpoints_DeleteRemoves()
    {
        using var factory = new TestFactory();
        var client = factory.CreateClient();

        var create = new ScheduleRequest
        {
            Enabled = true,
            StartTimeUtc = "07:00",
            RunSeconds = 30,
            DaysOfWeek = "Mon,Wed"
        };

        var createResponse = await client.PostAsJsonAsync("/api/schedules", create);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<IdResponse>();
        Assert.NotNull(created);

        var deleteResponse = await client.DeleteAsync($"/api/schedules/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var list = await client.GetFromJsonAsync<List<WateringSchedule>>("/api/schedules");
        Assert.NotNull(list);
        Assert.DoesNotContain(list!, item => item.Id == created.Id);
    }

    [Fact]
    public async Task ScheduleEndpoints_UpdateClearsLastRunDate()
    {
        using var factory = new TestFactory();
        var repository = factory.Services.GetRequiredService<ScheduleRepository>();
        var id = await repository.AddAsync(new WateringSchedule
        {
            Enabled = true,
            StartTimeUtc = "07:00",
            RunSeconds = 30,
            DaysOfWeek = "Mon",
            LastRunDateUtc = "2026-02-01"
        }, CancellationToken.None);

        var update = new ScheduleRequest
        {
            Enabled = true,
            StartTimeUtc = "07:15",
            RunSeconds = 35,
            DaysOfWeek = "Mon"
        };

        var client = factory.CreateClient();
        var response = await client.PutAsJsonAsync($"/api/schedules/{id}", update);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list = await client.GetFromJsonAsync<List<WateringSchedule>>("/api/schedules");
        Assert.NotNull(list);
        var schedule = Assert.Single(list!, item => item.Id == id);
        Assert.Null(schedule.LastRunDateUtc);
    }

    [Fact]
    public async Task AlarmsRecent_ReturnsOk()
    {
        using var factory = new TestFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/alarms/recent");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HistoryRecent_ReturnsOk()
    {
        using var factory = new TestFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/history/recent");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HistoryRecent_RespectsLimit()
    {
        using var factory = new TestFactory();
        var repository = factory.Services.GetRequiredService<RunHistoryRepository>();
        await repository.AddAsync(new RunHistoryEntry
        {
            ScheduleId = null,
            RequestedAtUtc = new DateTimeOffset(2026, 2, 2, 7, 0, 0, TimeSpan.Zero),
            RunSeconds = 10,
            Allowed = true,
            Reason = "test"
        }, CancellationToken.None);

        await repository.AddAsync(new RunHistoryEntry
        {
            ScheduleId = null,
            RequestedAtUtc = new DateTimeOffset(2026, 2, 2, 7, 1, 0, TimeSpan.Zero),
            RunSeconds = 12,
            Allowed = false,
            Reason = "blocked"
        }, CancellationToken.None);

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/history/recent?limit=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<List<RunHistoryEntry>>();
        Assert.NotNull(items);
        Assert.Single(items!);
    }

    [Fact]
    public async Task HistoryRecent_ReturnsMostRecentFirst()
    {
        using var factory = new TestFactory();
        var repository = factory.Services.GetRequiredService<RunHistoryRepository>();
        await repository.AddAsync(new RunHistoryEntry
        {
            ScheduleId = null,
            RequestedAtUtc = new DateTimeOffset(2026, 2, 2, 7, 0, 0, TimeSpan.Zero),
            RunSeconds = 10,
            Allowed = true,
            Reason = "first"
        }, CancellationToken.None);

        await repository.AddAsync(new RunHistoryEntry
        {
            ScheduleId = null,
            RequestedAtUtc = new DateTimeOffset(2026, 2, 2, 7, 1, 0, TimeSpan.Zero),
            RunSeconds = 12,
            Allowed = false,
            Reason = "second"
        }, CancellationToken.None);

        var client = factory.CreateClient();
        var items = await client.GetFromJsonAsync<List<RunHistoryEntry>>("/api/history/recent?limit=2");
        Assert.NotNull(items);
        Assert.Equal(2, items!.Count);
        Assert.Equal("second", items[0].Reason);
        Assert.Equal("first", items[1].Reason);
    }

    [Fact]
    public async Task AlarmsRecent_RespectsLimit()
    {
        using var factory = new TestFactory();
        var repository = factory.Services.GetRequiredService<AlarmRepository>();
        await repository.AddAsync(new AlarmRecord
        {
            Type = "LOW_WATER",
            Severity = "warning",
            Message = "first",
            RaisedAtUtc = new DateTimeOffset(2026, 2, 2, 7, 0, 0, TimeSpan.Zero),
            ReceivedAtUtc = new DateTimeOffset(2026, 2, 2, 7, 0, 0, TimeSpan.Zero)
        }, CancellationToken.None);

        await repository.AddAsync(new AlarmRecord
        {
            Type = "LEVEL_UNKNOWN",
            Severity = "warning",
            Message = "second",
            RaisedAtUtc = new DateTimeOffset(2026, 2, 2, 7, 1, 0, TimeSpan.Zero),
            ReceivedAtUtc = new DateTimeOffset(2026, 2, 2, 7, 1, 0, TimeSpan.Zero)
        }, CancellationToken.None);

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/alarms/recent?limit=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<List<SystemAlarmUpdate>>();
        Assert.NotNull(items);
        Assert.Single(items!);
    }

    [Fact]
    public async Task AlarmsRecent_ReturnsMostRecentFirst()
    {
        using var factory = new TestFactory();
        var repository = factory.Services.GetRequiredService<AlarmRepository>();
        await repository.AddAsync(new AlarmRecord
        {
            Type = "LOW_WATER",
            Severity = "warning",
            Message = "first",
            RaisedAtUtc = new DateTimeOffset(2026, 2, 2, 7, 0, 0, TimeSpan.Zero),
            ReceivedAtUtc = new DateTimeOffset(2026, 2, 2, 7, 0, 0, TimeSpan.Zero)
        }, CancellationToken.None);

        await repository.AddAsync(new AlarmRecord
        {
            Type = "LEVEL_UNKNOWN",
            Severity = "warning",
            Message = "second",
            RaisedAtUtc = new DateTimeOffset(2026, 2, 2, 7, 1, 0, TimeSpan.Zero),
            ReceivedAtUtc = new DateTimeOffset(2026, 2, 2, 7, 1, 0, TimeSpan.Zero)
        }, CancellationToken.None);

        var client = factory.CreateClient();
        var items = await client.GetFromJsonAsync<List<SystemAlarmUpdate>>("/api/alarms/recent?limit=2");
        Assert.NotNull(items);
        Assert.Equal(2, items!.Count);
        Assert.Equal("second", items[0].Message);
        Assert.Equal("first", items[1].Message);
    }

    [Fact]
    public async Task PumpStart_ReturnsConflictWhenWaterLevelUnknown()
    {
        using var factory = new TestFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/pump/start", new PumpStartRequest(10));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem!.Status);
        Assert.Contains("Water level", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PumpStart_ReturnsOkWhenSafe()
    {
        using var factory = new TestFactory();
        var store = factory.Services.GetRequiredService<WaterLevelStateStore>();
        store.Update(new WaterLevelStatePayload
        {
            LevelPercent = 50,
            Sensors = new[] { true, true, true, true },
            MeasuredAt = DateTimeOffset.UtcNow,
            ReportedAt = DateTimeOffset.UtcNow
        }, DateTimeOffset.UtcNow);

        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/pump/start", new PumpStartRequest(10));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PumpCommandResult>();
        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.Equal("manual", result.Reason);
        Assert.False(string.IsNullOrWhiteSpace(result.RequestId));
    }

    [Fact]
    public async Task PumpStart_ReturnsConflictWhenMqttDisconnected()
    {
        using var factory = new TestFactory(isConnected: false);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/pump/start", new PumpStartRequest(10));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem!.Status);
        Assert.Contains("MQTT", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PumpStop_ReturnsConflictWhenMqttDisconnected()
    {
        using var factory = new TestFactory(isConnected: false);
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/pump/stop", null);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem!.Status);
        Assert.Contains("MQTT", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WaterLevelLatest_ReturnsNoContentWhenMissing()
    {
        using var factory = new TestFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/waterlevel/latest");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task WaterLevelLatest_ReturnsLatestSnapshot()
    {
        using var factory = new TestFactory();
        var store = factory.Services.GetRequiredService<WaterLevelStateStore>();
        var now = new DateTimeOffset(2026, 2, 2, 7, 0, 0, TimeSpan.Zero);
        store.Update(new WaterLevelStatePayload
        {
            LevelPercent = 40,
            Sensors = new[] { true, false, false, false },
            MeasuredAt = now,
            ReportedAt = now
        }, now);

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/waterlevel/latest");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<WaterLevelUpdate>();
        Assert.NotNull(payload);
        Assert.Equal(40, payload!.LevelPercent);
    }

    [Fact]
    public async Task PumpLatest_ReturnsNoContentWhenMissing()
    {
        using var factory = new TestFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/pump/latest");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PumpLatest_ReturnsLatestSnapshot()
    {
        using var factory = new TestFactory();
        var store = factory.Services.GetRequiredService<PumpStateStore>();
        var now = new DateTimeOffset(2026, 2, 2, 7, 0, 0, TimeSpan.Zero);
        store.Update(new PumpStatePayload
        {
            Running = false,
            Since = null,
            LastRunSeconds = 20,
            LastRequestId = "abc",
            ReportedAt = now
        }, now);

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/pump/latest");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PumpStateUpdate>();
        Assert.NotNull(payload);
        Assert.False(payload!.Running);
        Assert.Equal(20, payload.LastRunSeconds);
    }

    [Fact]
    public async Task TestMqttEndpoints_ReturnConflictWhenDisconnected()
    {
        using var factory = new TestFactory(isConnected: false);
        var client = factory.CreateClient();
        var now = new DateTimeOffset(2026, 2, 2, 7, 0, 0, TimeSpan.Zero);

        var response = await client.PostAsJsonAsync("/api/test/mqtt/waterlevel", new WaterLevelStatePayload
        {
            LevelPercent = 10,
            Sensors = new[] { true, false, false, false },
            MeasuredAt = now,
            ReportedAt = now
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task TestMqttEndpoints_PublishesWhenConnected()
    {
        using var factory = new TestFactory();
        var publisher = factory.Services.GetRequiredService<IMqttPublisher>();
        var topics = factory.Services.GetRequiredService<MqttTopics>();
        var client = factory.CreateClient();
        var now = new DateTimeOffset(2026, 2, 2, 7, 0, 0, TimeSpan.Zero);

        var response = await client.PostAsJsonAsync("/api/test/mqtt/pumpstate", new PumpStatePayload
        {
            Running = false,
            Since = null,
            LastRunSeconds = 10,
            LastRequestId = "abc",
            ReportedAt = now
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        A.CallTo(() => publisher.PublishAsync(topics.PumpState, A<string>._, true, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    /// <summary>
    /// Minimal ID response shape from schedule create.
    /// </summary>
    private sealed record IdResponse(int Id);

    /// <summary>
    /// Minimal problem details payload for conflicts.
    /// </summary>
    private sealed record ProblemDetailsResponse(string? Type, string? Title, int? Status, string? Detail, string? Instance);

    /// <summary>
    /// Test server factory that overrides MQTT and storage dependencies.
    /// </summary>
    private sealed class TestFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"watering-api-{Guid.NewGuid():N}.db");
        private readonly IMqttPublisher _publisher;

        public TestFactory(bool isConnected = true)
        {
            _publisher = CreatePublisher(isConnected);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(Environments.Development);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.AddHostedService(sp => sp.GetRequiredService<DbInitializer>());
                services.AddSingleton<IMqttPublisher>(_publisher);
            });
            builder.ConfigureAppConfiguration((_, config) =>
            {
                var settings = new Dictionary<string, string?>
                {
                    ["Database:ConnectionString"] = $"Data Source={_dbPath}",
                    ["DevMqtt:AutoStart"] = "false",
                    ["Scheduling:CheckIntervalSeconds"] = "3600",
                    ["Mqtt:Host"] = "localhost",
                    ["Mqtt:Port"] = "1883",
                    ["Mqtt:UseTls"] = "false"
                };
                config.AddInMemoryCollection(settings);
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
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
        }

        private static IMqttPublisher CreatePublisher(bool isConnected)
        {
            var publisher = A.Fake<IMqttPublisher>();
            A.CallTo(() => publisher.IsConnected).Returns(isConnected);
            return publisher;
        }

    }
}
