using WateringController.Backend.Contracts;

namespace WateringController.Backend.State;

/// <summary>
/// Snapshot of the latest water level payload along with its receipt time.
/// </summary>
public sealed record WaterLevelSnapshot(
    WaterLevelStatePayload Payload,
    DateTimeOffset ReceivedAt
);
