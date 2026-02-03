using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using WateringController.Backend.Contracts;
using WateringController.Backend.Hubs;
using WateringController.Backend.State;

namespace WateringController.Backend.Mqtt;

/// <summary>
/// Parses pump state MQTT payloads, updates state, and broadcasts to SignalR clients.
/// </summary>
public sealed class PumpStateMqttHandler
{
    private readonly PumpStateStore _store;
    private readonly MqttTopics _topics;
    private readonly IHubContext<WateringHub> _hubContext;
    private readonly ILogger<PumpStateMqttHandler> _logger;

    public PumpStateMqttHandler(
        PumpStateStore store,
        MqttTopics topics,
        IHubContext<WateringHub> hubContext,
        ILogger<PumpStateMqttHandler> logger)
    {
        _store = store;
        _topics = topics;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Checks whether the topic matches the pump state topic.
    /// </summary>
    public bool CanHandle(string topic) =>
        string.Equals(topic, _topics.PumpState, StringComparison.Ordinal);

    /// <summary>
    /// Validates and applies a pump state update, then notifies connected clients.
    /// </summary>
    public async Task HandleAsync(string topic, ReadOnlyMemory<byte> payload, bool isRetained, DateTimeOffset receivedAt)
    {
        if (!CanHandle(topic))
        {
            _logger.LogWarning("Pump state handler received unexpected topic {Topic}.", topic);
            return;
        }

        if (!TryParsePayload(payload, out var parsed, out var error))
        {
            _logger.LogWarning("Invalid pump state payload: {Error}.", error);
            return;
        }

        _store.Update(parsed, receivedAt);

        var update = new PumpStateUpdate
        {
            Running = parsed.Running,
            Since = parsed.Since,
            LastRunSeconds = parsed.LastRunSeconds,
            LastRequestId = parsed.LastRequestId,
            ReportedAt = parsed.ReportedAt,
            ReceivedAt = receivedAt
        };

        await _hubContext.Clients.All.SendAsync("PumpStateUpdated", update);

        _logger.LogInformation(
            "Pump state update: running={Running} lastRunSeconds={LastRunSeconds} reportedAt={ReportedAt} retained={Retained}.",
            parsed.Running,
            parsed.LastRunSeconds,
            parsed.ReportedAt,
            isRetained);
    }

    private static bool TryParsePayload(
        ReadOnlyMemory<byte> json,
        out PumpStatePayload payload,
        out string error)
    {
        payload = new PumpStatePayload();
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

            if (!TryGetBool(document.RootElement, "running", out var running, out error))
            {
                return false;
            }

            DateTimeOffset? since = null;
            if (document.RootElement.TryGetProperty("since", out var sinceElement))
            {
                if (sinceElement.ValueKind == JsonValueKind.Null)
                {
                    since = null;
                }
                else
                {
                    if (sinceElement.ValueKind != JsonValueKind.String)
                    {
                        error = "since must be a UTC timestamp string.";
                        return false;
                    }

                    var text = sinceElement.GetString();
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        error = "since must not be empty.";
                        return false;
                    }

                    if (!DateTimeOffset.TryParse(text, out var parsedSince) || parsedSince.Offset != TimeSpan.Zero)
                    {
                        error = "since must be a valid UTC timestamp.";
                        return false;
                    }

                    since = parsedSince;
                }
            }

            if (running && since is null)
            {
                error = "since is required when running=true.";
                return false;
            }

            if (!TryGetInt(document.RootElement, "lastRunSeconds", out var lastRunSeconds, out error))
            {
                return false;
            }

            string? lastRequestId = null;
            if (document.RootElement.TryGetProperty("lastRequestId", out var requestIdElement))
            {
                if (requestIdElement.ValueKind == JsonValueKind.String)
                {
                    lastRequestId = requestIdElement.GetString();
                }
                else if (requestIdElement.ValueKind != JsonValueKind.Null)
                {
                    error = "lastRequestId must be a string.";
                    return false;
                }
            }

            if (!TryGetUtcTimestamp(document.RootElement, "reportedAt", out var reportedAt, out error))
            {
                return false;
            }

            payload = new PumpStatePayload
            {
                Running = running,
                Since = since,
                LastRunSeconds = lastRunSeconds,
                LastRequestId = lastRequestId,
                ReportedAt = reportedAt
            };

            return true;
        }
    }

    private static bool TryGetBool(JsonElement root, string property, out bool value, out string error)
    {
        value = false;
        error = string.Empty;

        if (!root.TryGetProperty(property, out var element))
        {
            error = $"Missing required property: {property}.";
            return false;
        }

        if (element.ValueKind != JsonValueKind.True && element.ValueKind != JsonValueKind.False)
        {
            error = $"{property} must be a boolean.";
            return false;
        }

        value = element.GetBoolean();
        return true;
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
