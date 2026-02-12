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
    private readonly ILogger<AlarmService> _logger;

    public AlarmService(
        IMqttPublisher publisher,
        AlarmStore store,
        MqttTopics topics,
        ILogger<AlarmService> logger)
    {
        _publisher = publisher;
        _store = store;
        _topics = topics;
        _logger = logger;
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
        _logger.LogWarning(
            "Raising alarm: type={Type} severity={Severity} raisedAt={RaisedAt}.",
            payload.Type,
            payload.Severity,
            payload.RaisedAt);
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
