namespace WateringController.Backend.Contracts;

/// <summary>
/// Command payload sent to the pump controller.
/// </summary>
public sealed record PumpCommandRequest
{
    public string Action { get; init; } = "start";
    public int? RunSeconds { get; init; }
    public string RequestId { get; init; } = Guid.NewGuid().ToString();
    public string Reason { get; init; } = "manual";
    public DateTimeOffset IssuedAt { get; init; } = DateTimeOffset.UtcNow;
}
