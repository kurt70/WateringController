namespace WateringController.Backend.Mqtt;

/// <summary>
/// Configuration for the embedded dev MQTT broker.
/// </summary>
public sealed class DevMqttOptions
{
    public const string SectionName = "DevMqtt";

    public bool AutoStart { get; init; } = true;
}
