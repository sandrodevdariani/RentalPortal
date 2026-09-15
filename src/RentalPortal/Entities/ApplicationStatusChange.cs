using RentalPortal.Domain;

namespace RentalPortal.Entities;

public class ApplicationStatusChange
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public RentalApplication Application { get; set; } = null!;
    public ApplicationStatus? FromStatus { get; set; }
    public ApplicationStatus ToStatus { get; set; }
    public ReviewOutcome? Outcome { get; set; }
    public string? Comment { get; set; }
    public string ChangedByUserId { get; set; } = string.Empty;
    public ApplicationUser ChangedBy { get; set; } = null!;
    public DateTimeOffset ChangedAt { get; set; }
}
