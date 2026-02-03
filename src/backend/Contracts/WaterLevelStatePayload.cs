namespace WateringController.Backend.Contracts;

/// <summary>
/// Raw water level payload received via MQTT.
/// </summary>
public sealed record WaterLevelStatePayload
{
    public int LevelPercent { get; init; }
    public bool[] Sensors { get; init; } = Array.Empty<bool>();
    public DateTimeOffset MeasuredAt { get; init; }
    public DateTimeOffset ReportedAt { get; init; }
}
