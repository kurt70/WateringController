using System.Text.Json;
using Microsoft.Extensions.Options;
using WateringController.Backend.Contracts;
using WateringController.Backend.Mqtt;
using WateringController.Backend.Options;
using WateringController.Backend.State;

namespace WateringController.Backend.Services;

/// <summary>
/// Issues pump commands with safety checks based on the latest water level state.
/// </summary>
public sealed class PumpCommandService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IMqttPublisher _publisher;
    private readonly WaterLevelStateStore _waterLevelStore;
    private readonly SafetyOptions _safetyOptions;
    private readonly TimeProvider _timeProvider;
    private readonly MqttTopics _topics;
    private readonly ILogger<PumpCommandService> _logger;

    public PumpCommandService(
        IMqttPublisher publisher,
        WaterLevelStateStore waterLevelStore,
        IOptions<SafetyOptions> safetyOptions,
        TimeProvider timeProvider,
        MqttTopics topics,
        ILogger<PumpCommandService> logger)
    {
        _publisher = publisher;
        _waterLevelStore = waterLevelStore;
        _safetyOptions = safetyOptions.Value;
        _timeProvider = timeProvider;
        _topics = topics;
        _logger = logger;
    }

    /// <summary>
    /// Issues a manual start command if safety checks pass.
    /// </summary>
    public async Task<PumpCommandResult> StartManualAsync(int runSeconds, CancellationToken cancellationToken)
    {
        if (runSeconds <= 0)
        {
            return new PumpCommandResult(false, "runSeconds must be greater than zero.", null);
        }

        if (!_publisher.IsConnected)
        {
            return new PumpCommandResult(false, "MQTT broker is not connected.", null, "mqtt_disconnected");
        }

        var safety = EvaluateSafety();
        if (!safety.Allowed)
        {
            return new PumpCommandResult(false, safety.Error, null, safety.Reason);
        }

        var command = new PumpCommandRequest
        {
            Action = "start",
            RunSeconds = runSeconds,
            Reason = "manual"
        };

        var payload = JsonSerializer.Serialize(command, JsonOptions);
        _logger.LogInformation("Publishing manual pump command: {Payload}", payload);
        await _publisher.PublishAsync(_topics.PumpCommand, payload, retain: false, cancellationToken);
        return new PumpCommandResult(true, null, command.RequestId, "manual");
    }

    /// <summary>
    /// Issues a manual stop command regardless of water level.
    /// </summary>
    public async Task<PumpCommandResult> StopManualAsync(CancellationToken cancellationToken)
    {
        if (!_publisher.IsConnected)
        {
            return new PumpCommandResult(false, "MQTT broker is not connected.", null, "mqtt_disconnected");
        }

        var command = new PumpCommandRequest
        {
            Action = "stop",
            RunSeconds = null,
            Reason = "manual_stop"
        };

        var payload = JsonSerializer.Serialize(command, JsonOptions);
        _logger.LogInformation("Publishing manual stop command: {Payload}", payload);
        await _publisher.PublishAsync(_topics.PumpCommand, payload, retain: false, cancellationToken);
        return new PumpCommandResult(true, null, command.RequestId, "manual_stop");
    }

    /// <summary>
    /// Issues a scheduled start command if safety checks pass.
    /// </summary>
    public async Task<PumpCommandResult> StartScheduledAsync(int runSeconds, CancellationToken cancellationToken)
    {
        if (runSeconds <= 0)
        {
            return new PumpCommandResult(false, "runSeconds must be greater than zero.", null, "invalid_duration");
        }

        if (!_publisher.IsConnected)
        {
            return new PumpCommandResult(false, "MQTT broker is not connected.", null, "mqtt_disconnected");
        }

        var safety = EvaluateSafety();
        if (!safety.Allowed)
        {
            return new PumpCommandResult(false, safety.Error, null, safety.Reason);
        }

        var command = new PumpCommandRequest
        {
            Action = "start",
            RunSeconds = runSeconds,
            Reason = "schedule"
        };

        var payload = JsonSerializer.Serialize(command, JsonOptions);
        _logger.LogInformation("Publishing scheduled pump command: {Payload}", payload);
        await _publisher.PublishAsync(_topics.PumpCommand, payload, retain: false, cancellationToken);
        return new PumpCommandResult(true, null, command.RequestId, "schedule");
    }

    /// <summary>
    /// Evaluates water level freshness and emptiness to block unsafe runs.
    /// </summary>
    private (bool Allowed, string? Error, string Reason) EvaluateSafety()
    {
        var latest = _waterLevelStore.GetLatest();
        if (latest is null)
        {
            return (false, "Water level is unknown.", "level_unknown");
        }

        var age = _timeProvider.GetUtcNow() - latest.ReceivedAt;
        if (age > TimeSpan.FromMinutes(_safetyOptions.WaterLevelStaleMinutes))
        {
            return (false, "Water level is stale.", "level_stale");
        }

        if (latest.Payload.LevelPercent <= 0)
        {
            return (false, "Water level is empty.", "level_empty");
        }

        return (true, null, "ok");
    }
}
