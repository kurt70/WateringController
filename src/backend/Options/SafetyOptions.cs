namespace WateringController.Backend.Options;

/// <summary>
/// Configuration for safety checks on pump operations.
/// </summary>
public sealed class SafetyOptions
{
    public const string SectionName = "Safety";

    public int WaterLevelStaleMinutes { get; init; } = 10;
}
