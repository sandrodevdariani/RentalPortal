using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RentalPortal.ViewModels;

public class UnitFormViewModel
{
    public int? Id { get; set; }
    public int PropertyId { get; set; }
    public string? PropertyName { get; set; }

    [Required, Display(Name = "Unit number"), StringLength(30)]
    public string UnitNumber { get; set; } = string.Empty;

    [Range(0, 20)]
    public int Bedrooms { get; set; }

    [Required, Display(Name = "Monthly rent"), Range(1, 100000)]
    public decimal MonthlyRent { get; set; }

    [Required, Display(Name = "Unit type")]
    public int UnitTypeId { get; set; }

    public IEnumerable<SelectListItem> UnitTypes { get; set; } = [];
}
