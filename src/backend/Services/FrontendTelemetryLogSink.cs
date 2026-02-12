using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using WateringController.Backend.Telemetry;

namespace WateringController.Backend.Services;

/// <summary>
/// Emits relayed frontend telemetry as OpenTelemetry logs under the frontend service identity.
/// </summary>
public sealed class FrontendTelemetryLogSink : IDisposable
{
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger<FrontendTelemetryLogSink> _fallbackLogger;
    private readonly ILogger? _frontendLogger;

    public FrontendTelemetryLogSink(IConfiguration configuration, ILogger<FrontendTelemetryLogSink> fallbackLogger)
    {
        _fallbackLogger = fallbackLogger;

        var enabled = configuration.GetValue<bool>("OpenTelemetry:Enabled");
        if (!enabled)
        {
            return;
        }

        var endpoint = configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";
        var serviceVersion = configuration["OpenTelemetry:ServiceVersion"] ?? "dev";

        _loggerFactory = LoggerFactory.Create(logging =>
        {
            logging.AddOpenTelemetry(options =>
            {
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
                options.ParseStateValues = true;
                options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(
                    serviceName: "WateringController.Frontend",
                    serviceVersion: serviceVersion));
                options.AddOtlpExporter(otlp =>
                {
                    otlp.Endpoint = new Uri(endpoint);
                    otlp.Protocol = OtlpExportProtocol.Grpc;
                });
            });
        });

        _frontendLogger = _loggerFactory.CreateLogger("FrontendTelemetry");
    }

    public void LogEvent(string eventName, Dictionary<string, object?>? attributes, string rawPayload)
    {
        var normalizedEvent = string.IsNullOrWhiteSpace(eventName) ? "frontend.event" : eventName;
        var site = attributes is not null && attributes.TryGetValue(TelemetryDimensions.Site, out var siteValue)
            ? siteValue?.ToString()
            : null;
        var source = attributes is not null && attributes.TryGetValue(TelemetryDimensions.EventSource, out var sourceValue)
            ? sourceValue?.ToString()
            : "frontend";

        if (_frontendLogger is not null)
        {
            _frontendLogger.LogInformation(
                "Frontend event received. event.name={EventName} event.source={EventSource} site={Site} payload={Payload}",
                normalizedEvent,
                source,
                site,
                rawPayload);
            return;
        }

        _fallbackLogger.LogInformation(
            "Frontend event received. event.name={EventName} event.source={EventSource} site={Site} payload={Payload}",
            normalizedEvent,
            source,
            site,
            rawPayload);
    }

    public void Dispose()
    {
        _loggerFactory?.Dispose();
    }
}
