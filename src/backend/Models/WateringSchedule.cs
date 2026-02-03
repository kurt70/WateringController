namespace WateringController.Backend.Models;

/// <summary>
/// Database record for a scheduled pump run configuration.
/// </summary>
public sealed record WateringSchedule
{
    public int Id { get; init; }
    public bool Enabled { get; init; }
    public string StartTimeUtc { get; init; } = "07:00";
    public int RunSeconds { get; init; }
    public string? DaysOfWeek { get; init; }
    public string? LastRunDateUtc { get; init; }
}
