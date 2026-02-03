using WateringController.Backend.Contracts;

namespace WateringController.Backend.State;

/// <summary>
/// Thread-safe storage for the latest water level snapshot.
/// </summary>
public sealed class WaterLevelStateStore
{
    private readonly object _lock = new();
    private WaterLevelSnapshot? _latest;

    public WaterLevelSnapshot? GetLatest()
    {
        lock (_lock)
        {
            return _latest;
        }
    }

    public void Update(WaterLevelStatePayload payload, DateTimeOffset receivedAt)
    {
        lock (_lock)
        {
            _latest = new WaterLevelSnapshot(payload, receivedAt);
        }
    }
}
