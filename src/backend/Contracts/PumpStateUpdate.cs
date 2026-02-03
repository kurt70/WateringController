namespace WateringController.Backend.Contracts;

/// <summary>
/// Pump state update sent to clients via SignalR and API.
/// </summary>
public sealed record PumpStateUpdate
{
    public bool Running { get; init; }
    public DateTimeOffset? Since { get; init; }
    public int LastRunSeconds { get; init; }
    public string? LastRequestId { get; init; }
    public DateTimeOffset ReportedAt { get; init; }
    public DateTimeOffset ReceivedAt { get; init; }
}
