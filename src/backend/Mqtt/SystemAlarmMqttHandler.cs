using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using WateringController.Backend.Contracts;
using WateringController.Backend.Hubs;
using WateringController.Backend.State;

namespace WateringController.Backend.Mqtt;

/// <summary>
/// Parses system alarm MQTT payloads, persists them, and broadcasts to SignalR clients.
/// </summary>
public sealed class SystemAlarmMqttHandler
{
    private readonly AlarmStore _store;
    private readonly MqttTopics _topics;
    private readonly IHubContext<WateringHub> _hubContext;
    private readonly ILogger<SystemAlarmMqttHandler> _logger;

    public SystemAlarmMqttHandler(
        AlarmStore store,
        MqttTopics topics,
        IHubContext<WateringHub> hubContext,
        ILogger<SystemAlarmMqttHandler> logger)
    {
        _store = store;
        _topics = topics;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Checks whether the topic matches the system alarm topic.
    /// </summary>
    public bool CanHandle(string topic) =>
        string.Equals(topic, _topics.SystemAlarm, StringComparison.Ordinal);

    /// <summary>
    /// Validates and applies an alarm update, then notifies connected clients.
    /// </summary>
    public async Task HandleAsync(string topic, ReadOnlyMemory<byte> payload, bool isRetained, DateTimeOffset receivedAt)
    {
        if (!CanHandle(topic))
        {
            _logger.LogWarning("System alarm handler received unexpected topic {Topic}.", topic);
            return;
        }

        if (!TryParsePayload(payload, out var parsed, out var error))
        {
            _logger.LogWarning("Invalid system alarm payload: {Error}.", error);
            return;
        }

        var update = new SystemAlarmUpdate
        {
            Type = parsed.Type,
            Severity = parsed.Severity,
            Message = parsed.Message,
            RaisedAt = parsed.RaisedAt,
            ReceivedAt = receivedAt
        };

        _store.Add(update);
        await _hubContext.Clients.All.SendAsync("AlarmRaised", update);

        _logger.LogWarning(
            "System alarm: type={Type} severity={Severity} raisedAt={RaisedAt} retained={Retained}.",
            parsed.Type,
            parsed.Severity,
            parsed.RaisedAt,
            isRetained);
    }

    private static bool TryParsePayload(
        ReadOnlyMemory<byte> json,
        out SystemAlarmPayload payload,
        out string error)
    {
        payload = new SystemAlarmPayload();
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

            if (!TryGetString(document.RootElement, "type", out var type, out error))
            {
                return false;
            }

            if (!TryGetString(document.RootElement, "severity", out var severity, out error))
            {
                return false;
            }

            if (!TryGetString(document.RootElement, "message", out var message, out error))
            {
                return false;
            }

            if (!TryGetUtcTimestamp(document.RootElement, "raisedAt", out var raisedAt, out error))
            {
                return false;
            }

            payload = new SystemAlarmPayload
            {
                Type = type,
                Severity = severity,
                Message = message,
                RaisedAt = raisedAt
            };

            return true;
        }
    }

    private static bool TryGetString(JsonElement root, string property, out string value, out string error)
    {
        value = string.Empty;
        error = string.Empty;

        if (!root.TryGetProperty(property, out var element))
        {
            error = $"Missing required property: {property}.";
            return false;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            error = $"{property} must be a string.";
            return false;
        }

        var text = element.GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            error = $"{property} must not be empty.";
            return false;
        }

        value = text;
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
