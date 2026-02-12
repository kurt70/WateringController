namespace WateringController.Backend.Mqtt;

/// <summary>
/// Configuration for connecting to the MQTT broker.
/// </summary>
public sealed class MqttOptions
{
    public const string SectionName = "Mqtt";

    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 1883;
    public bool UseTls { get; init; }
    public string? ClientId { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public int KeepAliveSeconds { get; init; } = 30;
    public int ReconnectSeconds { get; init; } = 5;
    public string TopicPrefix { get; init; } = "home/veranda";
}
