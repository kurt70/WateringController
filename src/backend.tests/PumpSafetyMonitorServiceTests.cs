using System.Reflection;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using WateringController.Backend.Contracts;
using WateringController.Backend.Mqtt;
using WateringController.Backend.Services;
using WateringController.Backend.State;
using Xunit;

namespace WateringController.Backend.Tests;

public sealed class PumpSafetyMonitorServiceTests
{
    [Fact]
    public async Task EvaluateRunningPumpSafetyAsync_IssuesStopAndAlarm_WhenLevelBecomesEmpty()
    {
        var publisher = A.Fake<IMqttPublisher>();
        A.CallTo(() => publisher.IsConnected).Returns(true);

        using var provider = new TestServiceProviderBuilder()
            .WithMqttPublisher(publisher)
            .Build();

        var pumpStore = provider.GetRequiredService<PumpStateStore>();
        var waterStore = provider.GetRequiredService<WaterLevelStateStore>();
        var topics = provider.GetRequiredService<MqttTopics>();

        var now = DateTimeOffset.UtcNow;
        pumpStore.Update(new PumpStatePayload
        {
            Running = true,
            Since = now.AddSeconds(-5),
            LastRunSeconds = 5,
            LastRequestId = "run-1",
            ReportedAt = now
        }, now);

        waterStore.Update(new WaterLevelStatePayload
        {
            LevelPercent = 0,
            Sensors = new[] { false, false, false, false },
            MeasuredAt = now,
            ReportedAt = now
        }, now);

        var monitor = provider.GetRequiredService<PumpSafetyMonitorService>();
        await InvokeEvaluateAsync(monitor);

        A.CallTo(() => publisher.PublishAsync(
                topics.PumpCommand,
                A<string>.That.Contains("safety_stop:level_empty"),
                false,
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => publisher.PublishAsync(
                topics.SystemAlarm,
                A<string>.That.Contains("LOW_WATER"),
                true,
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task EvaluateRunningPumpSafetyAsync_DoesNothing_WhenPumpNotRunning()
    {
        var publisher = A.Fake<IMqttPublisher>();
        A.CallTo(() => publisher.IsConnected).Returns(true);

        using var provider = new TestServiceProviderBuilder()
            .WithMqttPublisher(publisher)
            .Build();

        var pumpStore = provider.GetRequiredService<PumpStateStore>();
        var waterStore = provider.GetRequiredService<WaterLevelStateStore>();
        var topics = provider.GetRequiredService<MqttTopics>();

        var now = DateTimeOffset.UtcNow;
        pumpStore.Update(new PumpStatePayload
        {
            Running = false,
            Since = null,
            LastRunSeconds = 0,
            LastRequestId = null,
            ReportedAt = now
        }, now);

        waterStore.Update(new WaterLevelStatePayload
        {
            LevelPercent = 0,
            Sensors = new[] { false, false, false, false },
            MeasuredAt = now,
            ReportedAt = now
        }, now);

        var monitor = provider.GetRequiredService<PumpSafetyMonitorService>();
        await InvokeEvaluateAsync(monitor);

        A.CallTo(() => publisher.PublishAsync(topics.PumpCommand, A<string>._, false, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => publisher.PublishAsync(topics.SystemAlarm, A<string>._, true, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task EvaluateRunningPumpSafetyAsync_DeduplicatesStopAndAlarm_ForSameUnsafeReason()
    {
        var publisher = A.Fake<IMqttPublisher>();
        A.CallTo(() => publisher.IsConnected).Returns(true);

        using var provider = new TestServiceProviderBuilder()
            .WithMqttPublisher(publisher)
            .Build();

        var pumpStore = provider.GetRequiredService<PumpStateStore>();
        var waterStore = provider.GetRequiredService<WaterLevelStateStore>();
        var topics = provider.GetRequiredService<MqttTopics>();

        var now = DateTimeOffset.UtcNow;
        pumpStore.Update(new PumpStatePayload
        {
            Running = true,
            Since = now.AddSeconds(-10),
            LastRunSeconds = 10,
            LastRequestId = "run-2",
            ReportedAt = now
        }, now);

        waterStore.Update(new WaterLevelStatePayload
        {
            LevelPercent = 0,
            Sensors = new[] { false, false, false, false },
            MeasuredAt = now,
            ReportedAt = now
        }, now);

        var monitor = provider.GetRequiredService<PumpSafetyMonitorService>();
        await InvokeEvaluateAsync(monitor);
        await InvokeEvaluateAsync(monitor);

        A.CallTo(() => publisher.PublishAsync(topics.PumpCommand, A<string>._, false, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => publisher.PublishAsync(topics.SystemAlarm, A<string>._, true, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task EvaluateRunningPumpSafetyAsync_IssuesStopAndAlarm_WhenLevelBecomesUnknown()
    {
        var publisher = A.Fake<IMqttPublisher>();
        A.CallTo(() => publisher.IsConnected).Returns(true);

        using var provider = new TestServiceProviderBuilder()
            .WithMqttPublisher(publisher)
            .Build();

        var pumpStore = provider.GetRequiredService<PumpStateStore>();
        var topics = provider.GetRequiredService<MqttTopics>();

        var now = DateTimeOffset.UtcNow;
        pumpStore.Update(new PumpStatePayload
        {
            Running = true,
            Since = now.AddSeconds(-5),
            LastRunSeconds = 5,
            LastRequestId = "run-3",
            ReportedAt = now
        }, now);

        var monitor = provider.GetRequiredService<PumpSafetyMonitorService>();
        await InvokeEvaluateAsync(monitor);

        A.CallTo(() => publisher.PublishAsync(
                topics.PumpCommand,
                A<string>.That.Contains("safety_stop:level_unknown"),
                false,
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => publisher.PublishAsync(
                topics.SystemAlarm,
                A<string>.That.Contains("LEVEL_UNKNOWN"),
                true,
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    private static async Task InvokeEvaluateAsync(PumpSafetyMonitorService service)
    {
        var method = typeof(PumpSafetyMonitorService).GetMethod(
            "EvaluateRunningPumpSafetyAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        var task = (Task)method!.Invoke(service, new object[] { CancellationToken.None })!;
        await task;
    }
}
