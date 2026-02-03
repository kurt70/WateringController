using Microsoft.Extensions.Options;

namespace WateringController.Backend.Mqtt;

/// <summary>
/// Builds MQTT topic strings from configuration so publishers/subscribers stay consistent.
/// </summary>
public sealed class MqttTopics
{
    /// <summary>
    /// Constructs all topic names based on the configured prefix.
    /// </summary>
    public MqttTopics(IOptions<MqttOptions> options)
    {
        var prefix = (options.Value.TopicPrefix ?? "WateringController").Trim('/');
        if (string.IsNullOrWhiteSpace(prefix))
        {
            prefix = "WateringController";
        }

        var basePrefix = $"{prefix}/WateringController";
        PumpCommand = $"{basePrefix}/pump/cmd";
        PumpState = $"{basePrefix}/pump/state";
        WaterLevelState = $"{basePrefix}/waterlevel/state";
        SystemAlarm = $"{basePrefix}/system/alarm";
        SystemState = $"{basePrefix}/system/state";
    }

    public string PumpCommand { get; }
    public string PumpState { get; }
    public string WaterLevelState { get; }
    public string SystemAlarm { get; }
    public string SystemState { get; }
}
