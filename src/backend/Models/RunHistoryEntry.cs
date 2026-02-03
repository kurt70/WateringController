namespace WateringController.Backend.Models;

/// <summary>
/// Database record for pump run history.
/// </summary>
public sealed record RunHistoryEntry
{
    public int Id { get; init; }
    public int? ScheduleId { get; init; }
    public DateTimeOffset RequestedAtUtc { get; init; }
    public int RunSeconds { get; init; }
    public bool Allowed { get; init; }
    public string Reason { get; init; } = string.Empty;
}
