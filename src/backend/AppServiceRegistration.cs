using WateringController.Backend.Data;
using WateringController.Backend.Mqtt;
using WateringController.Backend.Options;
using WateringController.Backend.Services;
using WateringController.Backend.State;

namespace WateringController.Backend;

/// <summary>
/// Registers application services, options, and hosted services.
/// </summary>
public static class AppServiceRegistration
{
    public static void Register(WebApplicationBuilder builder)
    {
        builder.Services.AddSignalR();
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
        builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
        builder.Services.AddHostedService<DevMqttBrokerHostedService>();
        builder.Services.AddSingleton<MqttClientHostedService>();
        builder.Services.AddSingleton<IMqttPublisher>(sp => sp.GetRequiredService<MqttClientHostedService>());
        builder.Services.AddHostedService(sp => sp.GetRequiredService<MqttClientHostedService>());
        builder.Services.AddSingleton<ScheduleService>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<ScheduleService>());
    }
}
