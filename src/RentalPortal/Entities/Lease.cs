namespace RentalPortal.Entities;

public class Lease
{
    public int Id { get; set; }
    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;
    public int ApplicationId { get; set; }
    public RentalApplication Application { get; set; } = null!;
    public string TenantUserId { get; set; } = string.Empty;
    public ApplicationUser Tenant { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
