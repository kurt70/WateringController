using System.Text.Json;
using WateringController.Backend.Contracts;
using WateringController.Backend.Mqtt;
using WateringController.Backend.State;

namespace WateringController.Backend.Services;

/// <summary>
/// Raises alarms via MQTT and stores them locally for UI retrieval.
/// </summary>
public sealed class AlarmService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IMqttPublisher _publisher;
    private readonly AlarmStore _store;
    private readonly MqttTopics _topics;

    public AlarmService(IMqttPublisher publisher, AlarmStore store, MqttTopics topics)
    {
        _publisher = publisher;
        _store = store;
        _topics = topics;
    }

    /// <summary>
    /// Publishes an alarm (retained) and records it in the in-memory store.
    /// </summary>
    public async Task RaiseAsync(string type, string severity, string message, CancellationToken cancellationToken)
    {
        var payload = new SystemAlarmPayload
        {
            Type = type,
            Severity = severity,
            Message = message,
            RaisedAt = DateTimeOffset.UtcNow
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await _publisher.PublishAsync(_topics.SystemAlarm, json, retain: true, cancellationToken);

        _store.Add(new SystemAlarmUpdate
        {
            Type = payload.Type,
            Severity = payload.Severity,
            Message = payload.Message,
            RaisedAt = payload.RaisedAt,
            ReceivedAt = DateTimeOffset.UtcNow
        });
    }
}
