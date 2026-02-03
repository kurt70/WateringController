using WateringController.Backend.Contracts;
using WateringController.Backend.Data;
using WateringController.Backend.Models;

namespace WateringController.Backend.State;

/// <summary>
/// In-memory alarm cache with persistence to the alarm repository.
/// </summary>
public sealed class AlarmStore
{
    private readonly object _lock = new();
    private readonly List<SystemAlarmUpdate> _alarms = new();
    private readonly int _maxCount;
    private readonly AlarmRepository _repository;

    public AlarmStore(AlarmRepository repository, int maxCount = 50)
    {
        _repository = repository;
        _maxCount = maxCount;
    }

    public IReadOnlyList<SystemAlarmUpdate> GetRecent()
    {
        lock (_lock)
        {
            return _alarms.ToArray();
        }
    }

    public void Add(SystemAlarmUpdate alarm)
    {
        lock (_lock)
        {
            _alarms.Insert(0, alarm);
            if (_alarms.Count > _maxCount)
            {
                _alarms.RemoveAt(_alarms.Count - 1);
            }
        }

        _ = _repository.AddAsync(new AlarmRecord
        {
            Type = alarm.Type,
            Severity = alarm.Severity,
            Message = alarm.Message,
            RaisedAtUtc = alarm.RaisedAt,
            ReceivedAtUtc = alarm.ReceivedAt
        }, CancellationToken.None);
    }
}
