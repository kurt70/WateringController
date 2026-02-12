using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WateringController.Backend.Options;
using WateringController.Backend.State;

namespace WateringController.Backend.Services;

/// <summary>
/// Monitors running pump state and issues an automatic stop when water level becomes unsafe.
/// </summary>
public sealed class PumpSafetyMonitorService : BackgroundService
{
    private readonly PumpStateStore _pumpStateStore;
    private readonly WaterLevelStateStore _waterLevelStore;
    private readonly PumpCommandService _pumpCommandService;
    private readonly AlarmService _alarmService;
    private readonly SafetyOptions _safetyOptions;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PumpSafetyMonitorService> _logger;
    private string? _lastHandledUnsafeReason;

    public PumpSafetyMonitorService(
        PumpStateStore pumpStateStore,
        WaterLevelStateStore waterLevelStore,
        PumpCommandService pumpCommandService,
        AlarmService alarmService,
        IOptions<SafetyOptions> safetyOptions,
        TimeProvider timeProvider,
        ILogger<PumpSafetyMonitorService> logger)
    {
        _pumpStateStore = pumpStateStore;
        _waterLevelStore = waterLevelStore;
        _pumpCommandService = pumpCommandService;
        _alarmService = alarmService;
        _safetyOptions = safetyOptions.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = Math.Max(1, _safetyOptions.AutoStopCheckIntervalSeconds);
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(interval));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await EvaluateRunningPumpSafetyAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            timer.Dispose();
        }
    }

    private async Task EvaluateRunningPumpSafetyAsync(CancellationToken cancellationToken)
    {
        var pumpSnapshot = _pumpStateStore.GetLatest();
        if (pumpSnapshot is null || !pumpSnapshot.Payload.Running)
        {
            _lastHandledUnsafeReason = null;
            return;
        }

        var safety = EvaluateWaterLevelSafety();
        if (safety.Allowed)
        {
            _lastHandledUnsafeReason = null;
            return;
        }

        if (string.Equals(_lastHandledUnsafeReason, safety.Reason, StringComparison.Ordinal))
        {
            return;
        }

        var stopResult = await _pumpCommandService.StopForSafetyAsync(safety.Reason, cancellationToken);
        if (!stopResult.Success)
        {
            _logger.LogWarning(
                "Automatic safety stop failed while pump running. Reason={Reason} Error={Error}.",
                safety.Reason,
                stopResult.Error);
            return;
        }

        _lastHandledUnsafeReason = safety.Reason;
        var (alarmType, message) = MapAlarm(safety.Reason);
        await _alarmService.RaiseAsync(alarmType, "warning", message, cancellationToken);

        _logger.LogWarning(
            "Automatic safety stop triggered. Reason={Reason} requestId={RequestId}.",
            safety.Reason,
            stopResult.RequestId);
    }

    private (bool Allowed, string Reason) EvaluateWaterLevelSafety()
    {
        var latest = _waterLevelStore.GetLatest();
        if (latest is null)
        {
            return (false, "level_unknown");
        }

        var age = _timeProvider.GetUtcNow() - latest.ReceivedAt;
        if (age > TimeSpan.FromMinutes(_safetyOptions.WaterLevelStaleMinutes))
        {
            return (false, "level_stale");
        }

        if (latest.Payload.LevelPercent <= 0)
        {
            return (false, "level_empty");
        }

        return (true, "ok");
    }

    private static (string Type, string Message) MapAlarm(string reason)
    {
        return reason switch
        {
            "level_empty" => ("LOW_WATER", "Pump auto-stopped due to low water level"),
            "level_stale" => ("LEVEL_UNKNOWN", "Pump auto-stopped due to stale water level data"),
            _ => ("LEVEL_UNKNOWN", "Pump auto-stopped due to unknown water level")
        };
    }
}
