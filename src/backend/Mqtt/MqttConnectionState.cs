namespace WateringController.Backend.Mqtt;

/// <summary>
/// Tracks MQTT connection status for diagnostics and health checks.
/// </summary>
public sealed class MqttConnectionState
{
    private readonly object _lock = new();
    private bool _isConnected;
    private DateTimeOffset? _lastConnectedAt;
    private DateTimeOffset? _lastDisconnectedAt;

    public MqttConnectionSnapshot GetSnapshot()
    {
        lock (_lock)
        {
            return new MqttConnectionSnapshot(_isConnected, _lastConnectedAt, _lastDisconnectedAt);
        }
    }

    public void MarkConnected(DateTimeOffset at)
    {
        lock (_lock)
        {
            _isConnected = true;
            _lastConnectedAt = at;
        }
    }

    public void MarkDisconnected(DateTimeOffset at)
    {
        lock (_lock)
        {
            _isConnected = false;
            _lastDisconnectedAt = at;
        }
    }
}
