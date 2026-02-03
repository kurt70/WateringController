namespace WateringController.Frontend.Models;

public sealed record WaterLevelUpdate
{
    public int LevelPercent { get; init; }
    public bool[] Sensors { get; init; } = Array.Empty<bool>();
    public DateTimeOffset MeasuredAt { get; init; }
    public DateTimeOffset ReportedAt { get; init; }
    public DateTimeOffset ReceivedAt { get; init; }
}
