using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WateringController.Backend.Data;
using WateringController.Backend.Models;
using WateringController.Backend.Options;

namespace WateringController.Backend.Services;

/// <summary>
/// Evaluates schedules on an interval and issues pump commands if due.
/// </summary>
public sealed class ScheduleService : BackgroundService
{
    private readonly ScheduleRepository _scheduleRepository;
    private readonly RunHistoryRepository _historyRepository;
    private readonly PumpCommandService _pumpCommandService;
    private readonly AlarmService _alarmService;
    private readonly SchedulingOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ScheduleService> _logger;

    public ScheduleService(
        ScheduleRepository scheduleRepository,
        RunHistoryRepository historyRepository,
        PumpCommandService pumpCommandService,
        AlarmService alarmService,
        IOptions<SchedulingOptions> options,
        TimeProvider timeProvider,
        ILogger<ScheduleService> logger)
    {
        _scheduleRepository = scheduleRepository;
        _historyRepository = historyRepository;
        _pumpCommandService = pumpCommandService;
        _alarmService = alarmService;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Periodic loop that checks schedules and executes due entries.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.CheckIntervalSeconds));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await EvaluateSchedulesAsync(stoppingToken);
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

    /// <summary>
    /// Loads schedules, checks if due, and records history/alarms based on results.
    /// </summary>
    private async Task EvaluateSchedulesAsync(CancellationToken cancellationToken)
    {
        var schedules = await _scheduleRepository.GetAllAsync(cancellationToken);
        var nowUtc = _timeProvider.GetUtcNow();
        _logger.LogDebug("Schedule tick: nowUtc={NowUtc} schedules={Count}.", nowUtc, schedules.Count);
        if (schedules.Count == 0)
        {
            return;
        }
        foreach (var schedule in schedules)
        {
            if (!schedule.Enabled)
            {
                continue;
            }

            if (!IsDue(schedule, nowUtc))
            {
                continue;
            }

            _logger.LogInformation(
                "Schedule due: id={Id} start={StartTimeUtc} runSeconds={RunSeconds} days={DaysOfWeek}.",
                schedule.Id,
                schedule.StartTimeUtc,
                schedule.RunSeconds,
                schedule.DaysOfWeek);

            var result = await _pumpCommandService.StartScheduledAsync(schedule.RunSeconds, cancellationToken);
            var reason = result.Reason ?? (result.Success ? "schedule" : "blocked");

            _logger.LogInformation(
                "Schedule result: id={Id} success={Success} reason={Reason} requestId={RequestId}.",
                schedule.Id,
                result.Success,
                reason,
                result.RequestId);

            await _historyRepository.AddAsync(new RunHistoryEntry
            {
                ScheduleId = schedule.Id,
                RequestedAtUtc = nowUtc,
                RunSeconds = schedule.RunSeconds,
                Allowed = result.Success,
                Reason = reason
            }, cancellationToken);

            await _scheduleRepository.UpdateLastRunDateAsync(schedule.Id, nowUtc.ToString("yyyy-MM-dd"), cancellationToken);

            if (!result.Success && reason.StartsWith("level_", StringComparison.Ordinal))
            {
                var message = reason switch
                {
                    "level_empty" => "Pump run blocked due to low water level",
                    "level_stale" => "Pump run blocked due to stale water level data",
                    _ => "Pump run blocked due to unknown water level"
                };

                await _alarmService.RaiseAsync(reason == "level_empty" ? "LOW_WATER" : "LEVEL_UNKNOWN", "warning", message, cancellationToken);
            }

            if (!result.Success && reason == "mqtt_disconnected")
            {
                await _alarmService.RaiseAsync("MQTT_DISCONNECTED", "warning", "Pump run blocked due to MQTT disconnect", cancellationToken);
            }
        }
    }

    /// <summary>
    /// Determines if a schedule is due within the current interval window.
    /// </summary>
    private bool IsDue(WateringSchedule schedule, DateTimeOffset nowUtc)
    {
        if (!TimeSpan.TryParse(schedule.StartTimeUtc, out var startTime))
        {
            _logger.LogWarning("Invalid schedule time for schedule {Id}: {StartTimeUtc}", schedule.Id, schedule.StartTimeUtc);
            return false;
        }

        if (!IsAllowedDay(schedule.DaysOfWeek, nowUtc.DayOfWeek))
        {
            return false;
        }

        var today = nowUtc.ToString("yyyy-MM-dd");
        if (string.Equals(schedule.LastRunDateUtc, today, StringComparison.Ordinal))
        {
            return false;
        }

        var windowStart = startTime;
        var windowEnd = startTime + TimeSpan.FromSeconds(_options.CheckIntervalSeconds);
        var current = nowUtc.TimeOfDay;

        return current >= windowStart && current < windowEnd;
    }

    /// <summary>
    /// Parses day tokens (Mon,Tue,...) and checks if today is allowed.
    /// </summary>
    private static bool IsAllowedDay(string? daysOfWeek, DayOfWeek currentDay)
    {
        if (string.IsNullOrWhiteSpace(daysOfWeek))
        {
            return true;
        }

        var tokens = daysOfWeek.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
        {
            return true;
        }

        var currentToken = currentDay switch
        {
            DayOfWeek.Monday => "Mon",
            DayOfWeek.Tuesday => "Tue",
            DayOfWeek.Wednesday => "Wed",
            DayOfWeek.Thursday => "Thu",
            DayOfWeek.Friday => "Fri",
            DayOfWeek.Saturday => "Sat",
            DayOfWeek.Sunday => "Sun",
            _ => string.Empty
        };

        return tokens.Any(token => string.Equals(token, currentToken, StringComparison.OrdinalIgnoreCase));
    }
}
