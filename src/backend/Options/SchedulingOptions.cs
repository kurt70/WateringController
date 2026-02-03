namespace WateringController.Backend.Options;

/// <summary>
/// Configuration for schedule evaluation.
/// </summary>
public sealed class SchedulingOptions
{
    public const string SectionName = "Scheduling";

    public int CheckIntervalSeconds { get; init; } = 30;
}
