namespace WateringController.Backend.Contracts;

/// <summary>
/// Request body for creating or updating a schedule.
/// </summary>
public sealed record ScheduleRequest
{
    public bool Enabled { get; init; } = true;
    public string StartTimeUtc { get; init; } = "07:00";
    public int RunSeconds { get; init; } = 30;
    public string? DaysOfWeek { get; init; }
}
