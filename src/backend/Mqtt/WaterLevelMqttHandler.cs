using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using WateringController.Backend.Contracts;
using WateringController.Backend.Hubs;
using WateringController.Backend.State;

namespace WateringController.Backend.Mqtt;

/// <summary>
/// Parses water level MQTT payloads, updates state, and broadcasts to SignalR clients.
/// </summary>
public sealed class WaterLevelMqttHandler
{
    private const int ExpectedSensorCount = 4;
    private readonly WaterLevelStateStore _store;
    private readonly MqttTopics _topics;
    private readonly IHubContext<WateringHub> _hubContext;
    private readonly ILogger<WaterLevelMqttHandler> _logger;

    public WaterLevelMqttHandler(
        WaterLevelStateStore store,
        MqttTopics topics,
        IHubContext<WateringHub> hubContext,
        ILogger<WaterLevelMqttHandler> logger)
    {
        _store = store;
        _topics = topics;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Checks whether the topic matches the water level state topic.
    /// </summary>
    public bool CanHandle(string topic) =>
        string.Equals(topic, _topics.WaterLevelState, StringComparison.Ordinal);

    /// <summary>
    /// Validates and applies a water level update, then notifies connected clients.
    /// </summary>
    public async Task HandleAsync(string topic, ReadOnlyMemory<byte> payload, bool isRetained, DateTimeOffset receivedAt)
    {
        if (!CanHandle(topic))
        {
            _logger.LogWarning("Water level handler received unexpected topic {Topic}.", topic);
            return;
        }

        if (!TryParsePayload(payload, out var parsed, out var error))
        {
            _logger.LogWarning("Invalid water level payload: {Error}.", error);
            return;
        }

        _store.Update(parsed, receivedAt);

        var update = new WaterLevelUpdate
        {
            LevelPercent = parsed.LevelPercent,
            Sensors = parsed.Sensors,
            MeasuredAt = parsed.MeasuredAt,
            ReportedAt = parsed.ReportedAt,
            ReceivedAt = receivedAt
        };

        await _hubContext.Clients.All.SendAsync("WaterLevelUpdated", update);

        _logger.LogInformation(
            "Water level update: {LevelPercent}% sensors={Sensors} measuredAt={MeasuredAt} reportedAt={ReportedAt} retained={Retained}.",
            parsed.LevelPercent,
            string.Join(",", parsed.Sensors.Select(value => value ? "1" : "0")),
            parsed.MeasuredAt,
            parsed.ReportedAt,
            isRetained);
    }

    private static bool TryParsePayload(
        ReadOnlyMemory<byte> json,
        out WaterLevelStatePayload payload,
        out string error)
    {
        payload = new WaterLevelStatePayload();
        error = string.Empty;

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow
            });
        }
        catch (JsonException ex)
        {
            error = $"JSON parse error: {ex.Message}";
            return false;
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                error = "Payload root must be a JSON object.";
                return false;
            }

            if (!TryGetInt(document.RootElement, "levelPercent", out var levelPercent, out error))
            {
                return false;
            }

            if (levelPercent < 0 || levelPercent > 100)
            {
                error = "levelPercent must be between 0 and 100.";
                return false;
            }

            if (!document.RootElement.TryGetProperty("sensors", out var sensorsElement))
            {
                error = "Missing required property: sensors.";
                return false;
            }

            if (sensorsElement.ValueKind != JsonValueKind.Array)
            {
                error = "sensors must be an array of booleans.";
                return false;
            }

            var sensors = new bool[sensorsElement.GetArrayLength()];
            for (var i = 0; i < sensors.Length; i++)
            {
                var element = sensorsElement[i];
                if (element.ValueKind != JsonValueKind.True && element.ValueKind != JsonValueKind.False)
                {
                    error = "sensors must contain only boolean values.";
                    return false;
                }

                sensors[i] = element.GetBoolean();
            }

            if (sensors.Length != ExpectedSensorCount)
            {
                error = $"sensors must contain exactly {ExpectedSensorCount} values.";
                return false;
            }

            if (!TryGetUtcTimestamp(document.RootElement, "measuredAt", out var measuredAt, out error))
            {
                return false;
            }

            if (!TryGetUtcTimestamp(document.RootElement, "reportedAt", out var reportedAt, out error))
            {
                return false;
            }

            payload = new WaterLevelStatePayload
            {
                LevelPercent = levelPercent,
                Sensors = sensors,
                MeasuredAt = measuredAt,
                ReportedAt = reportedAt
            };

            return true;
        }
    }

    private static bool TryGetInt(JsonElement root, string property, out int value, out string error)
    {
        value = 0;
        error = string.Empty;

        if (!root.TryGetProperty(property, out var element))
        {
            error = $"Missing required property: {property}.";
            return false;
        }

        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out value))
        {
            error = $"{property} must be an integer.";
            return false;
        }

        return true;
    }

    private static bool TryGetUtcTimestamp(
        JsonElement root,
        string property,
        out DateTimeOffset value,
        out string error)
    {
        value = default;
        error = string.Empty;

        if (!root.TryGetProperty(property, out var element))
        {
            error = $"Missing required property: {property}.";
            return false;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            error = $"{property} must be a UTC timestamp string.";
            return false;
        }

        var text = element.GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            error = $"{property} must not be empty.";
            return false;
        }

        if (!DateTimeOffset.TryParse(text, out value))
        {
            error = $"{property} must be a valid UTC timestamp.";
            return false;
        }

        if (value.Offset != TimeSpan.Zero)
        {
            error = $"{property} must be in UTC (use Z suffix).";
            return false;
        }

        return true;
    }
}
