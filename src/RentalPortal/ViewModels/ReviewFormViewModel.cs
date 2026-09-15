using System.ComponentModel.DataAnnotations;
using RentalPortal.Domain;

namespace RentalPortal.ViewModels;

public class ReviewFormViewModel : IValidatableObject
{
    public int ApplicationId { get; set; }
    public string? ApplicantName { get; set; }
    public string? UnitLabel { get; set; }

    [Required]
    public ReviewOutcome Outcome { get; set; } = ReviewOutcome.Approve;

    [StringLength(1000)]
    public string? Comment { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Outcome is ReviewOutcome.Return or ReviewOutcome.Deny && string.IsNullOrWhiteSpace(Comment))
        {
            yield return new ValidationResult(
                "A comment is required when returning or denying an application.",
                [nameof(Comment)]);
        }
    }
}
