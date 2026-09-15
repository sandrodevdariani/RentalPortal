using RentalPortal.Domain;

namespace RentalPortal.Tests;

internal sealed class FakeClock : IClock
{
    public FakeClock(DateOnly today, DateTimeOffset? utcNow = null)
    {
        Today = today;
        UtcNow = utcNow ?? new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    }

    public DateOnly Today { get; }
    public DateTimeOffset UtcNow { get; }
}
