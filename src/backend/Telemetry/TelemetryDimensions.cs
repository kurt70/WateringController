namespace WateringController.Backend.Telemetry;

/// <summary>
/// Canonical telemetry property keys used across logs, traces, and relayed events.
/// </summary>
public static class TelemetryDimensions
{
    public const string Site = "site";
    public const string Component = "component";
    public const string RequestId = "request.id";
    public const string Route = "http.route";
    public const string ScheduleId = "schedule.id";
    public const string DeviceId = "device.id";
    public const string SafetyReason = "safety.reason";
    public const string EventName = "event.name";
    public const string EventSource = "event.source";
    public const string SentAt = "event.sent_at";
}
