using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using WateringController.Backend.Telemetry;

namespace WateringController.Backend.Services;

/// <summary>
/// Emits relayed frontend telemetry as OpenTelemetry traces under the frontend service identity.
/// </summary>
public sealed class FrontendTelemetryTraceSink : IDisposable
{
    private const string SourceName = "WateringController.Frontend.Relayed";

    private readonly ILogger<FrontendTelemetryTraceSink> _fallbackLogger;
    private readonly TracerProvider? _provider;
    private readonly ActivitySource? _source;

    public FrontendTelemetryTraceSink(IConfiguration configuration, ILogger<FrontendTelemetryTraceSink> fallbackLogger)
    {
        _fallbackLogger = fallbackLogger;

        var enabled = configuration.GetValue<bool>("OpenTelemetry:Enabled");
        if (!enabled)
        {
            return;
        }

        var endpoint = configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";
        var serviceVersion = configuration["OpenTelemetry:ServiceVersion"] ?? "dev";

        _source = new ActivitySource(SourceName);
        _provider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(
                serviceName: "WateringController.Frontend",
                serviceVersion: serviceVersion))
            .AddSource(SourceName)
            .AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri(endpoint);
                otlp.Protocol = OtlpExportProtocol.Grpc;
            })
            .Build();
    }

    public void TrackEvent(string eventName, Dictionary<string, object?>? attributes = null)
    {
        var name = string.IsNullOrWhiteSpace(eventName) ? "frontend.event" : eventName;
        using var activity = _source?.StartActivity(name, ActivityKind.Internal);
        if (activity is null)
        {
            _fallbackLogger.LogInformation("Frontend trace event (fallback): {EventName}", name);
            return;
        }

        if (attributes is null)
        {
            activity.SetTag(TelemetryDimensions.EventName, name);
            activity.SetTag(TelemetryDimensions.EventSource, "frontend");
            return;
        }

        activity.SetTag(TelemetryDimensions.EventName, name);
        if (!attributes.ContainsKey(TelemetryDimensions.EventSource))
        {
            activity.SetTag(TelemetryDimensions.EventSource, "frontend");
        }

        foreach (var pair in attributes)
        {
            if (pair.Value is null)
            {
                continue;
            }

            activity.SetTag(pair.Key, pair.Value.ToString());
        }
    }

    public void Dispose()
    {
        _provider?.Dispose();
        _source?.Dispose();
    }
}
