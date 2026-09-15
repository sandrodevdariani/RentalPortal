namespace RentalPortal.Domain;

public sealed class SystemClock : IClock
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
