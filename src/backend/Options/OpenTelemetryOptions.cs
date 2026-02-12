namespace WateringController.Backend.Options;

/// <summary>
/// Configuration for OpenTelemetry logs and metrics export.
/// </summary>
public sealed class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    public bool Enabled { get; init; }
    public string Site { get; init; } = "home/veranda";
    public string ServiceName { get; init; } = "WateringController.Backend";
    public string? ServiceVersion { get; init; }
    public string OtlpEndpoint { get; init; } = "http://localhost:4317";
    public bool ExportLogs { get; init; } = true;
    public bool ExportMetrics { get; init; } = true;
}
