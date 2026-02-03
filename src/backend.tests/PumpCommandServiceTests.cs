using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using WateringController.Backend.Contracts;
using WateringController.Backend.Mqtt;
using WateringController.Backend.Services;
using WateringController.Backend.State;
using Xunit;

namespace WateringController.Backend.Tests;

public sealed class PumpCommandServiceTests
{
    [Fact]
    public async Task StartManualAsync_BlocksWhenLevelUnknown()
    {
        var publisher = A.Fake<IMqttPublisher>();
        A.CallTo(() => publisher.IsConnected).Returns(true);

        using var provider = new TestServiceProviderBuilder()
            .WithMqttPublisher(publisher)
            .Build();

        var service = provider.GetRequiredService<PumpCommandService>();
        var result = await service.StartManualAsync(30, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("level_unknown", result.Reason);
    }

    [Fact]
    public async Task StartManualAsync_BlocksWhenLevelStale()
    {
        var publisher = A.Fake<IMqttPublisher>();
        A.CallTo(() => publisher.IsConnected).Returns(true);

        using var provider = new TestServiceProviderBuilder()
            .WithSetting("Safety:WaterLevelStaleMinutes", "1")
            .WithMqttPublisher(publisher)
            .Build();

        var store = provider.GetRequiredService<WaterLevelStateStore>();
        store.Update(new WaterLevelStatePayload
        {
            LevelPercent = 50,
            Sensors = new[] { true, true, true, true },
            MeasuredAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            ReportedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        }, DateTimeOffset.UtcNow.AddMinutes(-5));

        var service = provider.GetRequiredService<PumpCommandService>();
        var result = await service.StartManualAsync(30, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("level_stale", result.Reason);
    }

    [Fact]
    public async Task StartManualAsync_BlocksWhenLevelEmpty()
    {
        var publisher = A.Fake<IMqttPublisher>();
        A.CallTo(() => publisher.IsConnected).Returns(true);

        using var provider = new TestServiceProviderBuilder()
            .WithMqttPublisher(publisher)
            .Build();

        var store = provider.GetRequiredService<WaterLevelStateStore>();
        store.Update(new WaterLevelStatePayload
        {
            LevelPercent = 0,
            Sensors = new[] { false, false, false, false },
            MeasuredAt = DateTimeOffset.UtcNow,
            ReportedAt = DateTimeOffset.UtcNow
        }, DateTimeOffset.UtcNow);

        var service = provider.GetRequiredService<PumpCommandService>();
        var result = await service.StartManualAsync(30, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("level_empty", result.Reason);
    }

    [Fact]
    public async Task StartManualAsync_PublishesCommandWhenSafe()
    {
        var publisher = A.Fake<IMqttPublisher>();
        A.CallTo(() => publisher.IsConnected).Returns(true);

        using var provider = new TestServiceProviderBuilder()
            .WithMqttPublisher(publisher)
            .Build();

        var store = provider.GetRequiredService<WaterLevelStateStore>();
        store.Update(new WaterLevelStatePayload
        {
            LevelPercent = 50,
            Sensors = new[] { true, true, true, true },
            MeasuredAt = DateTimeOffset.UtcNow,
            ReportedAt = DateTimeOffset.UtcNow
        }, DateTimeOffset.UtcNow);

        var service = provider.GetRequiredService<PumpCommandService>();
        var result = await service.StartManualAsync(30, CancellationToken.None);

        Assert.True(result.Success);
        var topics = provider.GetRequiredService<WateringController.Backend.Mqtt.MqttTopics>();
        A.CallTo(() => publisher.PublishAsync(topics.PumpCommand, A<string>._, false, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }
}
