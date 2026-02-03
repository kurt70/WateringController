namespace WateringController.Backend.Contracts;

/// <summary>
/// Request body for starting the pump for a duration.
/// </summary>
public sealed record PumpStartRequest(int RunSeconds);
