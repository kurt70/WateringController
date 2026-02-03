using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WateringController.Backend.Mqtt;

/// <summary>
/// Starts an embedded Mosquitto broker for local development.
/// </summary>
public sealed class DevMqttBrokerHostedService : IHostedService
{
    private readonly IHostEnvironment _environment;
    private readonly DevMqttOptions _options;
    private readonly ILogger<DevMqttBrokerHostedService> _logger;
    private readonly string _contentRoot;
    private readonly string _composePath;

    public DevMqttBrokerHostedService(
        IHostEnvironment environment,
        IOptions<DevMqttOptions> options,
        ILogger<DevMqttBrokerHostedService> logger)
    {
        _environment = environment;
        _options = options.Value;
        _logger = logger;
        _contentRoot = environment.ContentRootPath;
        _composePath = Path.GetFullPath(Path.Combine(_contentRoot, "..", "..", "infra", "docker-compose.yml"));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment() || !_options.AutoStart)
        {
            return Task.CompletedTask;
        }

        if (!File.Exists(_composePath))
        {
            _logger.LogWarning("Docker compose file not found at {ComposePath}.", _composePath);
            return Task.CompletedTask;
        }

        TryRunCompose("up", "-d", "mqtt", "Starting MQTT broker via docker compose.");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment() || !_options.AutoStart)
        {
            return Task.CompletedTask;
        }

        if (!File.Exists(_composePath))
        {
            return Task.CompletedTask;
        }

        TryRunCompose("stop", "mqtt", "Stopping MQTT broker via docker compose.");
        return Task.CompletedTask;
    }

    private void TryRunCompose(string action, string arg1, string arg2, string logMessage)
    {
        TryRunCompose(action, new[] { arg1, arg2 }, logMessage);
    }

    private void TryRunCompose(string action, string arg1, string logMessage)
    {
        TryRunCompose(action, new[] { arg1 }, logMessage);
    }

    private void TryRunCompose(string action, string[] args, string logMessage)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            startInfo.ArgumentList.Add("compose");
            startInfo.ArgumentList.Add("-f");
            startInfo.ArgumentList.Add(_composePath);
            startInfo.ArgumentList.Add(action);
            foreach (var arg in args)
            {
                startInfo.ArgumentList.Add(arg);
            }

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                _logger.LogWarning("Failed to start docker compose process.");
                return;
            }

            _logger.LogInformation(logMessage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to run docker compose.");
        }
    }
}
