using WateringController.Backend.Contracts;

namespace WateringController.Backend.State;

public sealed record WaterLevelSnapshot(
    WaterLevelStatePayload Payload,
    DateTimeOffset ReceivedAt
);
