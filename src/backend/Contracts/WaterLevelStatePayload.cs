namespace WateringController.Backend.Contracts;

public sealed record WaterLevelStatePayload
{
    public int LevelPercent { get; init; }
    public bool[] Sensors { get; init; } = Array.Empty<bool>();
    public DateTimeOffset MeasuredAt { get; init; }
    public DateTimeOffset ReportedAt { get; init; }
}
