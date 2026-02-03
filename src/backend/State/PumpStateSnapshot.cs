using WateringController.Backend.Contracts;

namespace WateringController.Backend.State;

/// <summary>
/// Snapshot of the latest pump state along with its receipt time.
/// </summary>
public sealed record PumpStateSnapshot(
    PumpStatePayload Payload,
    DateTimeOffset ReceivedAt
);
