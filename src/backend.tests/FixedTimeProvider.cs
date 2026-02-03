using System;
using System.Diagnostics;

namespace WateringController.Backend.Tests;

public sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _fixedUtcNow;

    public FixedTimeProvider(DateTimeOffset fixedUtcNow)
    {
        _fixedUtcNow = fixedUtcNow;
    }

    public override DateTimeOffset GetUtcNow() => _fixedUtcNow;

    public override long GetTimestamp() => Stopwatch.GetTimestamp();

    public override long TimestampFrequency => Stopwatch.Frequency;
}
