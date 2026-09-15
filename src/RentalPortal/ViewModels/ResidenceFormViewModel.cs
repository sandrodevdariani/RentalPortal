using System.ComponentModel.DataAnnotations;

namespace RentalPortal.ViewModels;

public class ResidenceFormViewModel : IValidatableObject
{
    public int? Id { get; set; }
    public int ApplicationId { get; set; }

    [Required, StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [Required, Display(Name = "Landlord name"), StringLength(120)]
    public string LandlordName { get; set; } = string.Empty;

    [Required, Display(Name = "Landlord phone"), Phone, StringLength(40)]
    public string LandlordPhone { get; set; } = string.Empty;

    [Required, Display(Name = "Move-in date")]
    public DateOnly MoveInDate { get; set; }

    [Display(Name = "Move-out date")]
    public DateOnly? MoveOutDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MoveOutDate.HasValue && MoveOutDate.Value < MoveInDate)
        {
            yield return new ValidationResult(
                "Move-out date cannot be before move-in date.",
                [nameof(MoveOutDate)]);
        }
    }
}
