using Microsoft.Extensions.DependencyInjection;
using WateringController.Backend.Models;
using WateringController.Backend.Services;
using Xunit;

namespace WateringController.Backend.Tests;

public sealed class ScheduleServiceTests
{
    [Fact]
    public void IsDue_ReturnsTrueWithinWindow()
    {
        var service = CreateService(checkIntervalSeconds: 60);
        var schedule = new WateringSchedule
        {
            Enabled = true,
            StartTimeUtc = "07:00",
            RunSeconds = 30,
            DaysOfWeek = "Mon",
            LastRunDateUtc = null
        };

        var nowUtc = new DateTimeOffset(2024, 1, 1, 7, 0, 30, TimeSpan.Zero); // Monday

        Assert.True(InvokeIsDue(service, schedule, nowUtc));
    }

    [Fact]
    public void IsDue_ReturnsFalseWhenAlreadyRanToday()
    {
        var service = CreateService(checkIntervalSeconds: 60);
        var schedule = new WateringSchedule
        {
            Enabled = true,
            StartTimeUtc = "07:00",
            RunSeconds = 30,
            DaysOfWeek = "Mon",
            LastRunDateUtc = "2024-01-01"
        };

        var nowUtc = new DateTimeOffset(2024, 1, 1, 7, 0, 10, TimeSpan.Zero);

        Assert.False(InvokeIsDue(service, schedule, nowUtc));
    }

    [Fact]
    public void IsDue_ReturnsFalseWhenOutsideWindow()
    {
        var service = CreateService(checkIntervalSeconds: 30);
        var schedule = new WateringSchedule
        {
            Enabled = true,
            StartTimeUtc = "07:00",
            RunSeconds = 30,
            DaysOfWeek = "Mon",
            LastRunDateUtc = null
        };

        var nowUtc = new DateTimeOffset(2024, 1, 1, 7, 0, 45, TimeSpan.Zero);

        Assert.False(InvokeIsDue(service, schedule, nowUtc));
    }

    [Fact]
    public void IsDue_ReturnsFalseWhenDayNotAllowed()
    {
        var service = CreateService(checkIntervalSeconds: 60);
        var schedule = new WateringSchedule
        {
            Enabled = true,
            StartTimeUtc = "07:00",
            RunSeconds = 30,
            DaysOfWeek = "Tue",
            LastRunDateUtc = null
        };

        var nowUtc = new DateTimeOffset(2024, 1, 1, 7, 0, 10, TimeSpan.Zero); // Monday

        Assert.False(InvokeIsDue(service, schedule, nowUtc));
    }

    private static ScheduleService CreateService(int checkIntervalSeconds)
    {
        var provider = new TestServiceProviderBuilder()
            .WithSetting("Scheduling:CheckIntervalSeconds", checkIntervalSeconds.ToString())
            .Build();

        return provider.GetRequiredService<ScheduleService>();
    }

    private static bool InvokeIsDue(ScheduleService service, WateringSchedule schedule, DateTimeOffset nowUtc)
    {
        var method = typeof(ScheduleService).GetMethod("IsDue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);
        return (bool)method!.Invoke(service, new object[] { schedule, nowUtc })!;
    }
}
