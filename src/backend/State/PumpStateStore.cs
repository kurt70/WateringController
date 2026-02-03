using WateringController.Backend.Contracts;

namespace WateringController.Backend.State;

/// <summary>
/// Thread-safe storage for the latest pump state snapshot.
/// </summary>
public sealed class PumpStateStore
{
    private readonly object _lock = new();
    private PumpStateSnapshot? _latest;

    public PumpStateSnapshot? GetLatest()
    {
        lock (_lock)
        {
            return _latest;
        }
    }

    public void Update(PumpStatePayload payload, DateTimeOffset receivedAt)
    {
        lock (_lock)
        {
            _latest = new PumpStateSnapshot(payload, receivedAt);
        }
    }
}
