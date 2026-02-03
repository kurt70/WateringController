namespace WateringController.Backend.Options;

/// <summary>
/// Configuration for database connections.
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string ConnectionString { get; init; } = "Data Source=watering.db";
}
