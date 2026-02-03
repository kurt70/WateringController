namespace WateringController.Backend.Models;

/// <summary>
/// Database record for stored alarms.
/// </summary>
public sealed record AlarmRecord
{
    public int Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTimeOffset RaisedAtUtc { get; init; }
    public DateTimeOffset ReceivedAtUtc { get; init; }
}
