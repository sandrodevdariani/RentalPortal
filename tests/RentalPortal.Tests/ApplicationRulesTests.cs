using FluentAssertions;
using RentalPortal.Domain;

namespace RentalPortal.Tests;

public class ApplicationRulesTests
{
    [Theory]
    [InlineData(ApplicationStatus.Draft, true)]
    [InlineData(ApplicationStatus.Returned, true)]
    [InlineData(ApplicationStatus.Submitted, false)]
    [InlineData(ApplicationStatus.Approved, false)]
    [InlineData(ApplicationStatus.Denied, false)]
    [InlineData(ApplicationStatus.Withdrawn, false)]
    public void CanEdit_only_draft_and_returned(ApplicationStatus status, bool expected)
    {
        ApplicationRules.CanEdit(status).Should().Be(expected);
    }

    [Theory]
    [InlineData(ApplicationStatus.Approved, true)]
    [InlineData(ApplicationStatus.Denied, true)]
    [InlineData(ApplicationStatus.Withdrawn, true)]
    [InlineData(ApplicationStatus.Draft, false)]
    [InlineData(ApplicationStatus.Submitted, false)]
    [InlineData(ApplicationStatus.Returned, false)]
    public void Terminal_statuses(ApplicationStatus status, bool expected)
    {
        ApplicationRules.IsTerminal(status).Should().Be(expected);
    }

    [Fact]
    public void CanSubmit_requires_editable_status_and_both_sections()
    {
        ApplicationRules.CanSubmit(ApplicationStatus.Draft, true, true).Should().BeTrue();
        ApplicationRules.CanSubmit(ApplicationStatus.Draft, true, false).Should().BeFalse();
        ApplicationRules.CanSubmit(ApplicationStatus.Submitted, true, true).Should().BeFalse();
    }

    [Fact]
    public void CanReview_only_submitted()
    {
        ApplicationRules.CanReview(ApplicationStatus.Submitted).Should().BeTrue();
        ApplicationRules.CanReview(ApplicationStatus.Returned).Should().BeFalse();
    }

    [Fact]
    public void Active_lease_covers_today_and_excludes_end_date()
    {
        var today = new DateOnly(2026, 9, 15);
        var leases = new[] { (new DateOnly(2026, 9, 15), new DateOnly(2027, 9, 15)) };

        ApplicationRules.HasActiveLease(leases, today).Should().BeTrue();
        ApplicationRules.HasActiveLease(leases, new DateOnly(2027, 9, 15)).Should().BeFalse();
        ApplicationRules.HasActiveLease(leases, new DateOnly(2026, 9, 14)).Should().BeFalse();
    }

    [Fact]
    public void Lease_term_is_twelve_months()
    {
        ApplicationRules.LeaseEndDate(new DateOnly(2026, 9, 15))
            .Should().Be(new DateOnly(2027, 9, 15));
    }

    [Fact]
    public void Wizard_navigation()
    {
        ApplicationRules.NextSection(WizardSection.ApplicantInfo).Should().Be(WizardSection.ResidenceHistory);
        ApplicationRules.NextSection(WizardSection.ResidenceHistory).Should().Be(WizardSection.Summary);
        ApplicationRules.PreviousSection(WizardSection.Summary).Should().Be(WizardSection.ResidenceHistory);
        ApplicationRules.PreviousSection(WizardSection.ResidenceHistory).Should().Be(WizardSection.ApplicantInfo);
    }
}
