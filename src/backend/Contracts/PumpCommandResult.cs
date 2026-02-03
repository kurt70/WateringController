namespace WateringController.Backend.Contracts;

/// <summary>
/// Result returned from pump command operations.
/// </summary>
public sealed record PumpCommandResult(bool Success, string? Error, string? RequestId, string? Reason = null);
