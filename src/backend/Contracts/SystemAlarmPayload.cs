namespace WateringController.Backend.Contracts;

/// <summary>
/// Raw alarm payload received via MQTT.
/// </summary>
public sealed record SystemAlarmPayload
{
    public string Type { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTimeOffset RaisedAt { get; init; }
}
