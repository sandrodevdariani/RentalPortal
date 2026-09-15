namespace RentalPortal.Domain;

public static class ApplicationRules
{
    public static bool IsTerminal(ApplicationStatus status) =>
        status is ApplicationStatus.Approved
            or ApplicationStatus.Denied
            or ApplicationStatus.Withdrawn;

    public static bool CanEdit(ApplicationStatus status) =>
        status is ApplicationStatus.Draft or ApplicationStatus.Returned;

    public static bool CanWithdraw(ApplicationStatus status) =>
        status is ApplicationStatus.Draft
            or ApplicationStatus.Submitted
            or ApplicationStatus.Returned;

    public static bool CanReview(ApplicationStatus status) =>
        status is ApplicationStatus.Submitted;

    public static bool CanSubmit(ApplicationStatus status, bool applicantInfoSaved, bool residenceHistorySaved) =>
        CanEdit(status) && applicantInfoSaved && residenceHistorySaved;

    public static bool HasActiveLease(IEnumerable<(DateOnly Start, DateOnly End)> leases, DateOnly today) =>
        leases.Any(lease => lease.Start <= today && today < lease.End);

    public static DateOnly LeaseEndDate(DateOnly startDate) => startDate.AddMonths(12);

    public static WizardSection PreviousSection(WizardSection section) =>
        section switch
        {
            WizardSection.ResidenceHistory => WizardSection.ApplicantInfo,
            WizardSection.Summary => WizardSection.ResidenceHistory,
            _ => WizardSection.ApplicantInfo
        };

    public static WizardSection NextSection(WizardSection section) =>
        section switch
        {
            WizardSection.ApplicantInfo => WizardSection.ResidenceHistory,
            WizardSection.ResidenceHistory => WizardSection.Summary,
            _ => WizardSection.Summary
        };
}
