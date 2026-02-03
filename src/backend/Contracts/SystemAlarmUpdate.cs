namespace WateringController.Backend.Contracts;

/// <summary>
/// Alarm update sent to clients via SignalR and API.
/// </summary>
public sealed record SystemAlarmUpdate
{
    public string Type { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTimeOffset RaisedAt { get; init; }
    public DateTimeOffset ReceivedAt { get; init; }
}
