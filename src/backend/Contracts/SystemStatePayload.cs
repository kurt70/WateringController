namespace WateringController.Backend.Contracts;

/// <summary>
/// Aggregated system state payload published by the backend.
/// </summary>
public sealed record SystemStatePayload
{
    public bool PumpRunning { get; init; }
    public int WaterLevelPercent { get; init; }
    public bool SafeToRun { get; init; }
    public DateTimeOffset? NextScheduledRun { get; init; }
    public DateTimeOffset? LastRun { get; init; }
    public DateTimeOffset EvaluatedAt { get; init; }
}
