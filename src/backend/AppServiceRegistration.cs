using WateringController.Backend.Data;
using WateringController.Backend.Mqtt;
using WateringController.Backend.Options;
using WateringController.Backend.Services;
using WateringController.Backend.State;
using WateringController.Backend.Telemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace WateringController.Backend;

/// <summary>
/// Registers application services, options, and hosted services.
/// </summary>
public static class AppServiceRegistration
{
    public static void Register(WebApplicationBuilder builder)
    {
        builder.Services.AddSignalR();
        builder.Services.AddHttpClient();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy =>
            {
                policy
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .SetIsOriginAllowed(_ => true);
            });
        });

        builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));
        builder.Services.Configure<SafetyOptions>(builder.Configuration.GetSection(SafetyOptions.SectionName));
        builder.Services.Configure<SchedulingOptions>(builder.Configuration.GetSection(SchedulingOptions.SectionName));
        builder.Services.Configure<DevMqttOptions>(builder.Configuration.GetSection(DevMqttOptions.SectionName));
        builder.Services.Configure<MqttOptions>(builder.Configuration.GetSection(MqttOptions.SectionName));
        builder.Services.Configure<OpenTelemetryOptions>(builder.Configuration.GetSection(OpenTelemetryOptions.SectionName));

        ConfigureOpenTelemetry(builder);

        builder.Services.AddSingleton<SqliteConnectionFactory>();
        builder.Services.AddSingleton<ScheduleRepository>();
        builder.Services.AddSingleton<RunHistoryRepository>();
        builder.Services.AddSingleton<AlarmRepository>();
        builder.Services.AddSingleton<DbInitializer>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<DbInitializer>());

        builder.Services.AddSingleton<MqttTopics>();
        builder.Services.AddSingleton<WaterLevelStateStore>();
        builder.Services.AddSingleton<PumpStateStore>();
        builder.Services.AddSingleton<AlarmStore>();
        builder.Services.AddSingleton<WaterLevelMqttHandler>();
        builder.Services.AddSingleton<PumpStateMqttHandler>();
        builder.Services.AddSingleton<SystemAlarmMqttHandler>();
        builder.Services.AddSingleton<MqttConnectionState>();
        builder.Services.AddSingleton<PumpCommandService>();
        builder.Services.AddSingleton<AlarmService>();
        builder.Services.AddSingleton<FrontendTelemetryLogSink>();
        builder.Services.AddSingleton<FrontendTelemetryTraceSink>();
        builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
        builder.Services.AddHostedService<DevMqttBrokerHostedService>();
        builder.Services.AddSingleton<MqttClientHostedService>();
        builder.Services.AddSingleton<IMqttPublisher>(sp => sp.GetRequiredService<MqttClientHostedService>());
        builder.Services.AddHostedService(sp => sp.GetRequiredService<MqttClientHostedService>());
        builder.Services.AddSingleton<ScheduleService>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<ScheduleService>());
        builder.Services.AddSingleton<PumpSafetyMonitorService>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<PumpSafetyMonitorService>());
    }

    private static void ConfigureOpenTelemetry(WebApplicationBuilder builder)
    {
        var options = builder.Configuration
            .GetSection(OpenTelemetryOptions.SectionName)
            .Get<OpenTelemetryOptions>() ?? new OpenTelemetryOptions();

        if (!options.Enabled)
        {
            return;
        }

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: options.ServiceName, serviceVersion: options.ServiceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    [TelemetryDimensions.Site] = options.Site
                }))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(instrumentation =>
                    {
                        instrumentation.EnrichWithHttpRequest = (activity, request) =>
                        {
                            activity.SetTag(TelemetryDimensions.Site, options.Site);
                            activity.SetTag(TelemetryDimensions.Component, "backend");
                            activity.SetTag(TelemetryDimensions.RequestId, request.HttpContext.TraceIdentifier);
                            activity.SetTag(TelemetryDimensions.Route, request.Path.Value);
                        };

                        instrumentation.EnrichWithHttpResponse = (activity, response) =>
                        {
                            activity.SetTag(TelemetryDimensions.Site, options.Site);
                            activity.SetTag(TelemetryDimensions.Component, "backend");
                            activity.SetTag(TelemetryDimensions.RequestId, response.HttpContext.TraceIdentifier);
                        };
                    })
                    .AddHttpClientInstrumentation(instrumentation =>
                    {
                        instrumentation.EnrichWithHttpRequestMessage = (activity, _) =>
                        {
                            activity.SetTag(TelemetryDimensions.Site, options.Site);
                            activity.SetTag(TelemetryDimensions.Component, "backend");
                        };
                    });

                tracing.AddOtlpExporter(otlp =>
                {
                    otlp.Endpoint = new Uri(options.OtlpEndpoint);
                    otlp.Protocol = OtlpExportProtocol.Grpc;
                });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (options.ExportMetrics)
                {
                    metrics.AddOtlpExporter(otlp =>
                    {
                        otlp.Endpoint = new Uri(options.OtlpEndpoint);
                        otlp.Protocol = OtlpExportProtocol.Grpc;
                    });
                }
            });

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.ParseStateValues = true;
            logging.SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName: options.ServiceName, serviceVersion: options.ServiceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    [TelemetryDimensions.Site] = options.Site
                }));

            if (options.ExportLogs)
            {
                logging.AddOtlpExporter(otlp =>
                {
                    otlp.Endpoint = new Uri(options.OtlpEndpoint);
                    otlp.Protocol = OtlpExportProtocol.Grpc;
                });
            }
        });
    }
}
