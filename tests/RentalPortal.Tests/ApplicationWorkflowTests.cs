using FluentAssertions;
using RentalPortal.Domain;
using RentalPortal.Entities;

namespace RentalPortal.Tests;

public class ApplicationWorkflowTests
{
    private static readonly DateOnly Today = new(2026, 9, 15);

    private static ApplicationWorkflow Workflow() => new(new FakeClock(Today));

    private static RentalApplication Draft() => new()
    {
        UnitId = 10,
        ApplicantUserId = "applicant-1",
        Status = ApplicationStatus.Draft,
        ApplicantInfoSaved = true,
        ResidenceHistorySaved = true
    };

    [Fact]
    public void Continue_persists_applicant_info_only_when_editable()
    {
        var workflow = Workflow();
        var app = Draft();

        workflow.SaveApplicantInfo(app, "Jonah Reed", "206-555-0142", "j@example.com", "1 Pine St")
            .Succeeded.Should().BeTrue();
        app.FullName.Should().Be("Jonah Reed");
        app.ApplicantInfoSaved.Should().BeTrue();

        app.Status = ApplicationStatus.Submitted;
        workflow.SaveApplicantInfo(app, "Other", "1", "a@b.c", "x")
            .Succeeded.Should().BeFalse();
    }

    [Fact]
    public void Residence_history_requires_at_least_one_residence()
    {
        var workflow = Workflow();
        var app = Draft();

        workflow.SaveResidenceHistory(app, 0).Error.Should().Contain("residence");
        workflow.SaveResidenceHistory(app, 1).Succeeded.Should().BeTrue();
        app.ResidenceHistorySaved.Should().BeTrue();
    }

    [Fact]
    public void Submit_fails_when_unit_has_an_active_lease()
    {
        var workflow = Workflow();
        var app = Draft();
        var leases = new List<Lease>
        {
            new() { UnitId = 10, StartDate = Today.AddDays(-1), EndDate = Today.AddMonths(12) }
        };

        var result = workflow.Submit(app, leases, "applicant-1");

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("active lease");
        app.Status.Should().Be(ApplicationStatus.Draft);
    }

    [Fact]
    public void Submit_leaves_application_submitted_when_unit_is_free()
    {
        var workflow = Workflow();
        var app = Draft();

        workflow.Submit(app, [], "applicant-1").Succeeded.Should().BeTrue();
        app.Status.Should().Be(ApplicationStatus.Submitted);
        app.StatusChanges.Should().ContainSingle(c => c.ToStatus == ApplicationStatus.Submitted);
    }

    [Fact]
    public void Approve_issues_a_twelve_month_lease()
    {
        var workflow = Workflow();
        var app = Draft();
        app.Status = ApplicationStatus.Submitted;

        workflow.Review(app, ReviewOutcome.Approve, "Looks good", "manager-1", [])
            .Succeeded.Should().BeTrue();

        app.Status.Should().Be(ApplicationStatus.Approved);
        app.Lease.Should().NotBeNull();
        app.Lease!.StartDate.Should().Be(Today);
        app.Lease.EndDate.Should().Be(Today.AddMonths(12));
        app.Lease.UnitId.Should().Be(app.UnitId);
        app.Lease.TenantUserId.Should().Be(app.ApplicantUserId);
        app.StatusChanges.Should().Contain(c => c.Outcome == ReviewOutcome.Approve);
    }

    [Fact]
    public void Approve_rejects_a_second_lease_and_does_not_change_status()
    {
        var workflow = Workflow();
        var app = Draft();
        app.Status = ApplicationStatus.Submitted;
        var existing = new List<Lease>
        {
            new() { UnitId = 10, StartDate = Today, EndDate = Today.AddMonths(12) }
        };

        var result = workflow.Review(app, ReviewOutcome.Approve, null, "manager-1", existing);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("second lease");
        app.Status.Should().Be(ApplicationStatus.Submitted);
        app.Lease.Should().BeNull();
    }

    [Fact]
    public void Return_and_deny_require_a_comment()
    {
        var workflow = Workflow();
        var returned = Draft();
        returned.Status = ApplicationStatus.Submitted;
        workflow.Review(returned, ReviewOutcome.Return, " ", "manager-1", [])
            .Succeeded.Should().BeFalse();

        workflow.Review(returned, ReviewOutcome.Return, "Need another landlord.", "manager-1", [])
            .Succeeded.Should().BeTrue();
        returned.Status.Should().Be(ApplicationStatus.Returned);

        var denied = Draft();
        denied.Status = ApplicationStatus.Submitted;
        workflow.Review(denied, ReviewOutcome.Deny, "Income too low.", "manager-1", [])
            .Succeeded.Should().BeTrue();
        denied.Status.Should().Be(ApplicationStatus.Denied);
        ApplicationRules.IsTerminal(denied.Status).Should().BeTrue();
    }

    [Fact]
    public void Withdraw_is_blocked_from_terminal_states()
    {
        var workflow = Workflow();
        var app = Draft();
        app.Status = ApplicationStatus.Approved;

        workflow.Withdraw(app, "applicant-1").Succeeded.Should().BeFalse();

        app.Status = ApplicationStatus.Submitted;
        workflow.Withdraw(app, "applicant-1").Succeeded.Should().BeTrue();
        app.Status.Should().Be(ApplicationStatus.Withdrawn);
    }
}
