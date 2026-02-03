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

    private sealed record IdResponse(int Id);
    private sealed record ProblemDetailsResponse(string? Type, string? Title, int? Status, string? Detail, string? Instance);

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
