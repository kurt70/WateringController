namespace WateringController.Backend.Mqtt;

/// <summary>
/// Snapshot of the MQTT connection state.
/// </summary>
public sealed record MqttConnectionSnapshot(
    bool IsConnected,
    DateTimeOffset? LastConnectedAt,
    DateTimeOffset? LastDisconnectedAt
);
