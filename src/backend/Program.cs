using WateringController.Backend;
using WateringController.Backend.Mqtt;
using WateringController.Backend.State;
using WateringController.Backend.Hubs;
using WateringController.Backend.Contracts;
using WateringController.Backend.Services;
using System.Text.Json;
using WateringController.Backend.Data;
using WateringController.Backend.Options;
using WateringController.Backend.Models;
using WateringController.Backend.Telemetry;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
ApplyDatabaseOverrides(builder);
AppServiceRegistration.Register(builder);
var app = builder.Build();

app.UseCors("Frontend");

app.Use(async (context, next) =>
{
    var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
    var otelOptions = context.RequestServices.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;
    var logger = loggerFactory.CreateLogger("RequestScope");

    using (logger.BeginScope(new Dictionary<string, object?>
    {
        [TelemetryDimensions.Site] = otelOptions.Site,
        [TelemetryDimensions.Component] = "backend",
        [TelemetryDimensions.RequestId] = context.TraceIdentifier,
        [TelemetryDimensions.Route] = context.Request.Path.Value
    }))
    {
        await next();
    }
});

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// Health endpoint used by orchestration and diagnostics.
app.MapGet("/health", (MqttConnectionState connectionState) =>
{
    var snapshot = connectionState.GetSnapshot();
    return Results.Ok(new
    {
        status = snapshot.IsConnected ? "ok" : "degraded",
        mqtt = new
        {
            connected = snapshot.IsConnected,
            lastConnectedAt = snapshot.LastConnectedAt,
            lastDisconnectedAt = snapshot.LastDisconnectedAt
        }
    });
});

// SignalR hub for realtime UI updates.
app.MapHub<WateringHub>("/hubs/watering");

// Returns the latest cached water level snapshot (if available).
app.MapGet("/api/waterlevel/latest", (WaterLevelStateStore store) =>
{
    var latest = store.GetLatest();
    if (latest is null)
    {
        return Results.NoContent();
    }

    var payload = latest.Payload;
    var update = new WaterLevelUpdate
    {
        LevelPercent = payload.LevelPercent,
        Sensors = payload.Sensors,
        MeasuredAt = payload.MeasuredAt,
        ReportedAt = payload.ReportedAt,
        ReceivedAt = latest.ReceivedAt
    };

    return Results.Ok(update);
});

// Returns the latest cached pump state snapshot (if available).
app.MapGet("/api/pump/latest", (PumpStateStore store) =>
{
    var latest = store.GetLatest();
    if (latest is null)
    {
        return Results.NoContent();
    }

    var payload = latest.Payload;
    var update = new PumpStateUpdate
    {
        Running = payload.Running,
        Since = payload.Since,
        LastRunSeconds = payload.LastRunSeconds,
        LastRequestId = payload.LastRequestId,
        ReportedAt = payload.ReportedAt,
        ReceivedAt = latest.ReceivedAt
    };

    return Results.Ok(update);
});

// Returns recent alarms from storage (most recent first).
app.MapGet("/api/alarms/recent", async (AlarmRepository repository, int? limit, CancellationToken cancellationToken) =>
{
    var items = await repository.GetRecentAsync(limit ?? 50, cancellationToken);
    var updates = items.Select(item => new SystemAlarmUpdate
    {
        Type = item.Type,
        Severity = item.Severity,
        Message = item.Message,
        RaisedAt = item.RaisedAtUtc,
        ReceivedAt = item.ReceivedAtUtc
    });
    return Results.Ok(updates);
});

// Returns recent run history entries.
app.MapGet("/api/history/recent", async (RunHistoryRepository repository, int? limit, CancellationToken cancellationToken) =>
{
    var items = await repository.GetRecentAsync(limit ?? 50, cancellationToken);
    return Results.Ok(items);
});

// Lists all schedules.
app.MapGet("/api/schedules", async (ScheduleRepository repository, CancellationToken cancellationToken) =>
{
    var items = await repository.GetAllAsync(cancellationToken);
    return Results.Ok(items);
});

// Creates a new schedule after basic validation.
app.MapPost("/api/schedules", async (ScheduleRepository repository, ScheduleRequest request, ILogger<Program> logger, CancellationToken cancellationToken) =>
{
    logger.LogInformation("Schedule create requested: {ScheduleJson}", JsonSerializer.Serialize(request));
    if (request.RunSeconds <= 0)
    {
        return Results.BadRequest("runSeconds must be greater than zero.");
    }

    if (!TimeSpan.TryParse(request.StartTimeUtc, out _))
    {
        return Results.BadRequest("startTimeUtc must be HH:mm or HH:mm:ss.");
    }

    var id = await repository.AddAsync(new WateringSchedule
    {
        Enabled = request.Enabled,
        StartTimeUtc = request.StartTimeUtc,
        RunSeconds = request.RunSeconds,
        DaysOfWeek = request.DaysOfWeek
    }, cancellationToken);

    return Results.Ok(new { id });
});

// Updates an existing schedule after basic validation.
app.MapPut("/api/schedules/{id:int}", async (int id, ScheduleRepository repository, ScheduleRequest request, ILogger<Program> logger, CancellationToken cancellationToken) =>
{
    logger.LogInformation("Schedule update requested for {Id}: {ScheduleJson}", id, JsonSerializer.Serialize(request));
    if (request.RunSeconds <= 0)
    {
        return Results.BadRequest("runSeconds must be greater than zero.");
    }

    if (!TimeSpan.TryParse(request.StartTimeUtc, out _))
    {
        return Results.BadRequest("startTimeUtc must be HH:mm or HH:mm:ss.");
    }

    await repository.UpdateAsync(new WateringSchedule
    {
        Id = id,
        Enabled = request.Enabled,
        StartTimeUtc = request.StartTimeUtc,
        RunSeconds = request.RunSeconds,
        DaysOfWeek = request.DaysOfWeek
    }, cancellationToken);

    await repository.ClearLastRunDateAsync(id, cancellationToken);
    logger.LogInformation("Schedule {Id} lastRunDateUtc cleared after update.", id);

    return Results.Ok();
});

// Deletes a schedule by id.
app.MapDelete("/api/schedules/{id:int}", async (int id, ScheduleRepository repository, CancellationToken cancellationToken) =>
{
    await repository.DeleteAsync(id, cancellationToken);
    return Results.Ok();
});

// Manual pump start endpoint with safety checks.
app.MapPost("/api/pump/start", async (PumpCommandService service, PumpStartRequest request, CancellationToken cancellationToken) =>
{
    var result = await service.StartManualAsync(request.RunSeconds, cancellationToken);
    return result.Success
        ? Results.Ok(result)
        : Results.Problem(result.Error, statusCode: StatusCodes.Status409Conflict);
});

// Manual safety stop endpoint.
app.MapPost("/api/pump/stop", async (PumpCommandService service, CancellationToken cancellationToken) =>
{
    var result = await service.StopManualAsync(cancellationToken);
    return result.Success
        ? Results.Ok(result)
        : Results.Problem(result.Error, statusCode: StatusCodes.Status409Conflict);
});

// Receives frontend telemetry fallback events and logs them server-side.
app.MapPost("/api/otel/client-event", async (HttpRequest request, FrontendTelemetryLogSink logSink, FrontendTelemetryTraceSink traceSink, IOptions<OpenTelemetryOptions> otelOptionsMonitor, CancellationToken cancellationToken) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync(cancellationToken);
    var site = otelOptionsMonitor.Value.Site;

    try
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var name = root.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
            ? nameElement.GetString() ?? "frontend.event"
            : "frontend.event";

        Dictionary<string, object?> attributes = new(StringComparer.Ordinal)
        {
            [TelemetryDimensions.Site] = site,
            [TelemetryDimensions.Component] = "frontend",
            [TelemetryDimensions.EventName] = name,
            [TelemetryDimensions.EventSource] = "frontend"
        };

        if (root.TryGetProperty("attributes", out var attributesElement) && attributesElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in attributesElement.EnumerateObject())
            {
                attributes[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Number => property.Value.TryGetInt64(out var l) ? l : property.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => property.Value.ToString()
                };
            }
        }

        if (root.TryGetProperty("sentAt", out var sentAtElement) && sentAtElement.ValueKind == JsonValueKind.String)
        {
            attributes[TelemetryDimensions.SentAt] = sentAtElement.GetString();
        }

        logSink.LogEvent(name, attributes, body);
        traceSink.TrackEvent(name, attributes);
    }
    catch
    {
        var attributes = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [TelemetryDimensions.Site] = site,
            [TelemetryDimensions.Component] = "frontend",
            [TelemetryDimensions.EventName] = "frontend.event.parse_error",
            [TelemetryDimensions.EventSource] = "frontend"
        };
        logSink.LogEvent("frontend.event.parse_error", attributes, body);
        traceSink.TrackEvent("frontend.event.parse_error", attributes);
    }

    return Results.Accepted();
});

// Same-origin proxy for browser OTLP traces to avoid CORS/preflight issues.
app.MapPost("/api/otel/v1/traces", async (HttpRequest request, IHttpClientFactory httpClientFactory, IConfiguration configuration, CancellationToken cancellationToken) =>
{
    var target = configuration["OpenTelemetry:FrontendTraceProxyTarget"] ?? "http://localhost:4318/v1/traces";

    using var ms = new MemoryStream();
    await request.Body.CopyToAsync(ms, cancellationToken);
    ms.Position = 0;

    using var forwardRequest = new HttpRequestMessage(HttpMethod.Post, target)
    {
        Content = new ByteArrayContent(ms.ToArray())
    };

    if (!string.IsNullOrWhiteSpace(request.ContentType))
    {
        forwardRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(request.ContentType);
    }

    using var client = httpClientFactory.CreateClient();
    using var response = await client.SendAsync(forwardRequest, cancellationToken);
    return Results.StatusCode((int)response.StatusCode);
});

if (app.Environment.IsDevelopment())
{
    var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    // Test helpers that publish retained/non-retained MQTT messages in dev only.
    app.MapPost("/api/test/mqtt/waterlevel", async (IMqttPublisher publisher, MqttTopics topics, WaterLevelStatePayload payload, CancellationToken cancellationToken) =>
    {
        if (!publisher.IsConnected)
        {
            return Results.Problem("MQTT broker is not connected.", statusCode: StatusCodes.Status409Conflict);
        }

        var json = JsonSerializer.Serialize(payload, jsonOptions);
        await publisher.PublishAsync(topics.WaterLevelState, json, retain: true, cancellationToken);
        return Results.Ok();
    });

    app.MapPost("/api/test/mqtt/pumpstate", async (IMqttPublisher publisher, MqttTopics topics, PumpStatePayload payload, CancellationToken cancellationToken) =>
    {
        if (!publisher.IsConnected)
        {
            return Results.Problem("MQTT broker is not connected.", statusCode: StatusCodes.Status409Conflict);
        }

        var json = JsonSerializer.Serialize(payload, jsonOptions);
        await publisher.PublishAsync(topics.PumpState, json, retain: true, cancellationToken);
        return Results.Ok();
    });

    app.MapPost("/api/test/mqtt/alarm", async (IMqttPublisher publisher, MqttTopics topics, SystemAlarmPayload payload, CancellationToken cancellationToken) =>
    {
        if (!publisher.IsConnected)
        {
            return Results.Problem("MQTT broker is not connected.", statusCode: StatusCodes.Status409Conflict);
        }

        var json = JsonSerializer.Serialize(payload, jsonOptions);
        await publisher.PublishAsync(topics.SystemAlarm, json, retain: true, cancellationToken);
        return Results.Ok();
    });

    app.MapPost("/api/test/mqtt/systemstate", async (IMqttPublisher publisher, MqttTopics topics, SystemStatePayload payload, CancellationToken cancellationToken) =>
    {
        if (!publisher.IsConnected)
        {
            return Results.Problem("MQTT broker is not connected.", statusCode: StatusCodes.Status409Conflict);
        }

        var json = JsonSerializer.Serialize(payload, jsonOptions);
        await publisher.PublishAsync(topics.SystemState, json, retain: true, cancellationToken);
        return Results.Ok();
    });

    app.MapPost("/api/test/mqtt/pumpcmd", async (IMqttPublisher publisher, MqttTopics topics, PumpCommandRequest payload, CancellationToken cancellationToken) =>
    {
        if (!publisher.IsConnected)
        {
            return Results.Problem("MQTT broker is not connected.", statusCode: StatusCodes.Status409Conflict);
        }

        var json = JsonSerializer.Serialize(payload, jsonOptions);
        await publisher.PublishAsync(topics.PumpCommand, json, retain: false, cancellationToken);
        return Results.Ok();
    });
}

app.MapFallbackToFile("index.html");

app.Run();

static void ApplyDatabaseOverrides(WebApplicationBuilder builder)
{
    var dbPath = Environment.GetEnvironmentVariable("WATERING_DB_PATH");
    if (!string.IsNullOrWhiteSpace(dbPath))
    {
        builder.Configuration["Database:ConnectionString"] = $"Data Source={dbPath}";
    }
}

/// <summary>
/// Marker class required for integration testing.
/// </summary>
public partial class Program { }
