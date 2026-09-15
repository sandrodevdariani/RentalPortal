using System.ComponentModel.DataAnnotations;

namespace RentalPortal.ViewModels;

public class PropertyFormViewModel
{
    public int? Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Address { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string State { get; set; } = string.Empty;

    [Required, Display(Name = "ZIP code"), StringLength(20)]
    public string ZipCode { get; set; } = string.Empty;
}
