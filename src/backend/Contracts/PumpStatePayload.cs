namespace WateringController.Backend.Contracts;

/// <summary>
/// Raw pump state payload received via MQTT.
/// </summary>
public sealed record PumpStatePayload
{
    public bool Running { get; init; }
    public DateTimeOffset? Since { get; init; }
    public int LastRunSeconds { get; init; }
    public string? LastRequestId { get; init; }
    public DateTimeOffset ReportedAt { get; init; }
}
