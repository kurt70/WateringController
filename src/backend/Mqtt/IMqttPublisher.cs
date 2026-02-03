namespace WateringController.Backend.Mqtt;

public interface IMqttPublisher
{
    bool IsConnected { get; }
    Task PublishAsync(string topic, string payload, bool retain, CancellationToken cancellationToken);
}
