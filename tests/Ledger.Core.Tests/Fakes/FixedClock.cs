using Ledger.Core.Abstractions;

namespace Ledger.Core.Tests.Fakes;

internal sealed class FixedClock : IClock
{
    public FixedClock(DateTimeOffset utcNow) { UtcNow = utcNow; }

    public DateTimeOffset UtcNow { get; }
    public DateOnly Today => DateOnly.FromDateTime(UtcNow.UtcDateTime);
}
