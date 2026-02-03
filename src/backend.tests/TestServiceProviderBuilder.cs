using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WateringController.Backend;
using WateringController.Backend.Mqtt;

namespace WateringController.Backend.Tests;

public sealed class TestServiceProviderBuilder
{
    private readonly Dictionary<string, string?> _settings = new()
    {
        ["Database:ConnectionString"] = "Data Source=watering.tests.db",
        ["Safety:WaterLevelStaleMinutes"] = "10",
        ["Scheduling:CheckIntervalSeconds"] = "30",
        ["Mqtt:Host"] = "localhost",
        ["Mqtt:Port"] = "1883",
        ["Mqtt:UseTls"] = "false",
        ["DevMqtt:AutoStart"] = "false"
    };

    private IMqttPublisher? _publisherOverride;
    private TimeProvider? _timeProviderOverride;

    public TestServiceProviderBuilder WithSetting(string key, string value)
    {
        _settings[key] = value;
        return this;
    }

    public TestServiceProviderBuilder WithMqttPublisher(IMqttPublisher publisher)
    {
        _publisherOverride = publisher;
        return this;
    }

    public TestServiceProviderBuilder WithTimeProvider(TimeProvider timeProvider)
    {
        _timeProviderOverride = timeProvider;
        return this;
    }

    public ServiceProvider Build()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(_settings);
        builder.Environment.EnvironmentName = Environments.Development;

        AppServiceRegistration.Register(builder);

        if (_publisherOverride is not null)
        {
            builder.Services.AddSingleton<IMqttPublisher>(_publisherOverride);
        }

        if (_timeProviderOverride is not null)
        {
            builder.Services.AddSingleton(_timeProviderOverride);
        }

        return builder.Services.BuildServiceProvider();
    }
}
