using RentalPortal.Entities;

namespace RentalPortal.Domain;

public sealed class ApplicationWorkflow
{
    private readonly IClock _clock;

    public ApplicationWorkflow(IClock clock)
    {
        _clock = clock;
    }

    public bool HasActiveLease(IEnumerable<Lease> leases) =>
        ApplicationRules.HasActiveLease(leases.Select(l => (l.StartDate, l.EndDate)), _clock.Today);

    public WorkflowResult SaveApplicantInfo(RentalApplication application, string fullName, string phone, string email, string currentAddress)
    {
        if (!ApplicationRules.CanEdit(application.Status))
        {
            return WorkflowResult.Fail("This application can no longer be edited.");
        }

        application.FullName = fullName.Trim();
        application.Phone = phone.Trim();
        application.Email = email.Trim();
        application.CurrentAddress = currentAddress.Trim();
        application.ApplicantInfoSaved = true;
        application.UpdatedAt = _clock.UtcNow;
        return WorkflowResult.Ok();
    }

    public WorkflowResult SaveResidenceHistory(RentalApplication application, int residenceCount)
    {
        if (!ApplicationRules.CanEdit(application.Status))
        {
            return WorkflowResult.Fail("This application can no longer be edited.");
        }

        if (residenceCount < 1)
        {
            return WorkflowResult.Fail("Add at least one prior residence before continuing.");
        }

        application.ResidenceHistorySaved = true;
        application.UpdatedAt = _clock.UtcNow;
        return WorkflowResult.Ok();
    }

    public WorkflowResult Submit(RentalApplication application, IReadOnlyCollection<Lease> unitLeases, string userId)
    {
        if (!ApplicationRules.CanSubmit(application.Status, application.ApplicantInfoSaved, application.ResidenceHistorySaved))
        {
            return WorkflowResult.Fail("Both sections must be saved before the application can be submitted.");
        }

        if (HasActiveLease(unitLeases))
        {
            return WorkflowResult.Fail("This unit already has an active lease and cannot accept applications.");
        }

        Transition(application, ApplicationStatus.Submitted, userId, comment: null, outcome: null);
        return WorkflowResult.Ok();
    }

    public WorkflowResult Withdraw(RentalApplication application, string userId)
    {
        if (!ApplicationRules.CanWithdraw(application.Status))
        {
            return WorkflowResult.Fail("This application cannot be withdrawn.");
        }

        Transition(application, ApplicationStatus.Withdrawn, userId, comment: null, outcome: null);
        return WorkflowResult.Ok();
    }

    public WorkflowResult Review(
        RentalApplication application,
        ReviewOutcome outcome,
        string? comment,
        string reviewerId,
        IReadOnlyCollection<Lease> unitLeases)
    {
        if (!ApplicationRules.CanReview(application.Status))
        {
            return WorkflowResult.Fail("Only submitted applications can be reviewed.");
        }

        if (outcome is ReviewOutcome.Return or ReviewOutcome.Deny && string.IsNullOrWhiteSpace(comment))
        {
            return WorkflowResult.Fail("A comment is required when returning or denying an application.");
        }

        if (outcome == ReviewOutcome.Approve)
        {
            if (HasActiveLease(unitLeases))
            {
                return WorkflowResult.Fail("This unit already has an active lease. Approval would create a second lease.");
            }

            Transition(application, ApplicationStatus.Approved, reviewerId, comment, ReviewOutcome.Approve);
            var start = _clock.Today;
            application.Lease = new Lease
            {
                UnitId = application.UnitId,
                TenantUserId = application.ApplicantUserId,
                StartDate = start,
                EndDate = ApplicationRules.LeaseEndDate(start)
            };
            return WorkflowResult.Ok();
        }

        if (outcome == ReviewOutcome.Return)
        {
            Transition(application, ApplicationStatus.Returned, reviewerId, comment, ReviewOutcome.Return);
            return WorkflowResult.Ok();
        }

        Transition(application, ApplicationStatus.Denied, reviewerId, comment, ReviewOutcome.Deny);
        return WorkflowResult.Ok();
    }

    public void RecordCreated(RentalApplication application, string userId)
    {
        application.StatusChanges.Add(new ApplicationStatusChange
        {
            FromStatus = null,
            ToStatus = ApplicationStatus.Draft,
            ChangedByUserId = userId,
            ChangedAt = _clock.UtcNow,
            Comment = "Application created."
        });
    }

    private void Transition(
        RentalApplication application,
        ApplicationStatus toStatus,
        string userId,
        string? comment,
        ReviewOutcome? outcome)
    {
        var from = application.Status;
        application.Status = toStatus;
        application.UpdatedAt = _clock.UtcNow;
        application.StatusChanges.Add(new ApplicationStatusChange
        {
            FromStatus = from,
            ToStatus = toStatus,
            Outcome = outcome,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            ChangedByUserId = userId,
            ChangedAt = _clock.UtcNow
        });
    }
}
