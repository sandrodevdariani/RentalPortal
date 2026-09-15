using RentalPortal.Domain;

namespace RentalPortal.Entities;

public class RentalApplication
{
    public int Id { get; set; }
    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;
    public string ApplicantUserId { get; set; } = string.Empty;
    public ApplicationUser Applicant { get; set; } = null!;
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;

    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CurrentAddress { get; set; } = string.Empty;
    public bool ApplicantInfoSaved { get; set; }

    public bool ResidenceHistorySaved { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Residence> Residences { get; set; } = new List<Residence>();
    public ICollection<ApplicationStatusChange> StatusChanges { get; set; } = new List<ApplicationStatusChange>();
    public Lease? Lease { get; set; }
}
