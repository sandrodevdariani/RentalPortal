using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RentalPortal.Domain;

namespace RentalPortal.ViewModels;

public class ApplicationWizardViewModel
{
    public int Id { get; set; }
    public int UnitId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; }
    public bool CanEdit { get; set; }
    public bool CanWithdraw { get; set; }
    public bool CanReview { get; set; }
    public bool ApplicantInfoSaved { get; set; }
    public bool ResidenceHistorySaved { get; set; }
    public WizardSection CurrentSection { get; set; } = WizardSection.ApplicantInfo;

    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Phone")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Current address")]
    public string CurrentAddress { get; set; } = string.Empty;

    public List<ResidenceRowViewModel> Residences { get; set; } = [];

    public bool IsApplicantInfoEditable => CanEdit && CurrentSection == WizardSection.ApplicantInfo;
    public bool IsResidenceHistoryEditable => CanEdit && CurrentSection == WizardSection.ResidenceHistory;

    public void ValidateApplicantInfo(ModelStateDictionary modelState)
    {
        if (string.IsNullOrWhiteSpace(FullName))
        {
            modelState.AddModelError(nameof(FullName), "Name is required.");
        }

        if (string.IsNullOrWhiteSpace(Phone))
        {
            modelState.AddModelError(nameof(Phone), "Phone is required.");
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            modelState.AddModelError(nameof(Email), "Email is required.");
        }
        else if (!new EmailAddressAttribute().IsValid(Email))
        {
            modelState.AddModelError(nameof(Email), "Enter a valid email address.");
        }

        if (string.IsNullOrWhiteSpace(CurrentAddress))
        {
            modelState.AddModelError(nameof(CurrentAddress), "Current address is required.");
        }
    }
}
