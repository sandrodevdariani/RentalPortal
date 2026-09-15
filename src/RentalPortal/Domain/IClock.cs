namespace RentalPortal.Domain;

public interface IClock
{
    DateOnly Today { get; }
    DateTimeOffset UtcNow { get; }
}
