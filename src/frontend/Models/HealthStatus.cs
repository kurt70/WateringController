namespace WateringController.Frontend.Models;

public sealed record HealthStatus
{
    public string? Status { get; init; }
    public MqttHealth? Mqtt { get; init; }
}

public sealed record MqttHealth
{
    public bool Connected { get; init; }
    public DateTimeOffset? LastConnectedAt { get; init; }
    public DateTimeOffset? LastDisconnectedAt { get; init; }
}
