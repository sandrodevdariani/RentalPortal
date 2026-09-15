using Microsoft.AspNetCore.Mvc.Rendering;
using RentalPortal.Domain;

namespace RentalPortal.ViewModels;

public class ApplicationListViewModel
{
    public ApplicationStatus? Status { get; set; }
    public int? PropertyId { get; set; }
    public IEnumerable<SelectListItem> Statuses { get; set; } = [];
    public IEnumerable<SelectListItem> Properties { get; set; } = [];
    public IReadOnlyList<ApplicationListItemViewModel> Items { get; set; } = [];
}

public class ApplicationListItemViewModel
{
    public int Id { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
