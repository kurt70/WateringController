namespace WateringController.Backend.Contracts;

/// <summary>
/// Water level update sent to clients via SignalR and API.
/// </summary>
public sealed record WaterLevelUpdate
{
    public int LevelPercent { get; init; }
    public bool[] Sensors { get; init; } = Array.Empty<bool>();
    public DateTimeOffset MeasuredAt { get; init; }
    public DateTimeOffset ReportedAt { get; init; }
    public DateTimeOffset ReceivedAt { get; init; }
}
