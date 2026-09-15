using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Data;
using RentalPortal.Domain;
using RentalPortal.Entities;
using RentalPortal.Infrastructure;
using RentalPortal.ViewModels;

namespace RentalPortal.Controllers;

[Authorize]
public class ApplicationsController : ModalController
{
    private readonly AppDbContext _db;
    private readonly ApplicationWorkflow _workflow;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IClock _clock;

    public ApplicationsController(
        AppDbContext db,
        ApplicationWorkflow workflow,
        UserManager<ApplicationUser> userManager,
        IClock clock)
    {
        _db = db;
        _workflow = workflow;
        _userManager = userManager;
        _clock = clock;
    }

    public async Task<IActionResult> Index(ApplicationStatus? status, int? propertyId)
    {
        var userId = _userManager.GetUserId(User)!;
        var isManager = User.IsInRole(Roles.PropertyManager);

        var query = _db.Applications
            .AsNoTracking()
            .Include(a => a.Unit)
            .ThenInclude(u => u.Property)
            .AsQueryable();

        if (!isManager)
        {
            query = query.Where(a => a.ApplicantUserId == userId);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        if (propertyId.HasValue)
        {
            query = query.Where(a => a.Unit.PropertyId == propertyId.Value);
        }

        var items = await query
            .OrderByDescending(a => a.UpdatedAt)
            .Select(a => new ApplicationListItemViewModel
            {
                Id = a.Id,
                ApplicantName = a.Applicant.FirstName + " " + a.Applicant.LastName,
                PropertyName = a.Unit.Property.Name,
                UnitNumber = a.Unit.UnitNumber,
                Status = a.Status,
                UpdatedAt = a.UpdatedAt
            })
            .ToListAsync();

        var vm = new ApplicationListViewModel
        {
            Status = status,
            PropertyId = propertyId,
            Items = items,
            Statuses = Enum.GetValues<ApplicationStatus>()
                .Select(s => new SelectListItem(s.ToString(), ((int)s).ToString(), status == s)),
            Properties = await _db.Properties
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .Select(p => new SelectListItem(p.Name, p.Id.ToString(), propertyId == p.Id))
                .ToListAsync()
        };

        return View(vm);
    }

    [Authorize(Roles = Roles.Applicant)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int unitId)
    {
        var userId = _userManager.GetUserId(User)!;

        var unit = await _db.Units
            .Include(u => u.Leases)
            .FirstOrDefaultAsync(u => u.Id == unitId);
        if (unit is null)
        {
            return NotFound();
        }

        if (_workflow.HasActiveLease(unit.Leases))
        {
            TempData["Error"] = "This unit already has an active lease and is not available.";
            return RedirectToAction("Available", "Units");
        }

        var open = await _db.Applications.FirstOrDefaultAsync(a =>
            a.UnitId == unitId &&
            a.ApplicantUserId == userId &&
            a.Status != ApplicationStatus.Approved &&
            a.Status != ApplicationStatus.Denied &&
            a.Status != ApplicationStatus.Withdrawn);

        if (open is not null)
        {
            return RedirectToAction(nameof(Edit), new { id = open.Id });
        }

        var user = await _userManager.GetUserAsync(User);
        var application = new RentalApplication
        {
            UnitId = unitId,
            ApplicantUserId = userId,
            Status = ApplicationStatus.Draft,
            FullName = user?.FullName ?? string.Empty,
            Phone = user?.PhoneNumber ?? string.Empty,
            Email = user?.Email ?? string.Empty,
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow
        };
        _workflow.RecordCreated(application, userId);
        _db.Applications.Add(application);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Edit), new { id = application.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, WizardSection section = WizardSection.ApplicantInfo)
    {
        var application = await LoadApplicationAsync(id);
        if (application is null)
        {
            return NotFound();
        }

        if (!CanView(application))
        {
            return Forbid();
        }

        return View(ToWizard(application, section));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ApplicationWizardViewModel model, string intent)
    {
        var application = await LoadApplicationAsync(id);
        if (application is null)
        {
            return NotFound();
        }

        if (!CanView(application))
        {
            return Forbid();
        }

        intent = (intent ?? string.Empty).ToLowerInvariant();

        if (intent == "back")
        {
            return RedirectToAction(nameof(Edit), new { id, section = ApplicationRules.PreviousSection(model.CurrentSection) });
        }

        var canEdit = CanApplicantEdit(application);

        if (intent == "continue")
        {
            if (!canEdit)
            {
                return RedirectToAction(nameof(Edit), new { id, section = ApplicationRules.NextSection(model.CurrentSection) });
            }

            if (model.CurrentSection == WizardSection.ApplicantInfo)
            {
                model.ValidateApplicantInfo(ModelState);
                if (!ModelState.IsValid)
                {
                    return View(ReloadWizard(application, model));
                }

                var saved = _workflow.SaveApplicantInfo(application, model.FullName, model.Phone, model.Email, model.CurrentAddress);
                if (!saved.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, saved.Error!);
                    return View(ReloadWizard(application, model));
                }

                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Edit), new { id, section = WizardSection.ResidenceHistory });
            }

            if (model.CurrentSection == WizardSection.ResidenceHistory)
            {
                var saved = _workflow.SaveResidenceHistory(application, application.Residences.Count);
                if (!saved.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, saved.Error!);
                    return View(ReloadWizard(application, model));
                }

                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Edit), new { id, section = WizardSection.Summary });
            }

            return RedirectToAction(nameof(Edit), new { id, section = WizardSection.Summary });
        }

        if (intent == "submit")
        {
            if (!canEdit)
            {
                ModelState.AddModelError(string.Empty, "This application can no longer be submitted.");
                return View(ReloadWizard(application, model));
            }

            if (model.CurrentSection != WizardSection.Summary)
            {
                ModelState.AddModelError(string.Empty, "Submit is only available from the summary.");
                return View(ReloadWizard(application, model));
            }

            var leases = await _db.Leases.Where(l => l.UnitId == application.UnitId).ToListAsync();
            var result = _workflow.Submit(application, leases, _userManager.GetUserId(User)!);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error!);
                return View(ReloadWizard(application, model));
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Application submitted.";
            return RedirectToAction(nameof(Edit), new { id, section = WizardSection.Summary });
        }

        return View(ReloadWizard(application, model));
    }

    [Authorize(Roles = Roles.Applicant)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(int id)
    {
        var application = await LoadApplicationAsync(id);
        if (application is null)
        {
            return NotFound();
        }

        if (application.ApplicantUserId != _userManager.GetUserId(User))
        {
            return Forbid();
        }

        var result = _workflow.Withdraw(application, _userManager.GetUserId(User)!);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Edit), new { id });
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Application withdrawn.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.PropertyManager)]
    [HttpGet]
    public async Task<IActionResult> ReviewModal(int id)
    {
        var application = await LoadApplicationAsync(id);
        if (application is null)
        {
            return NotFound();
        }

        var reviewModel = new ReviewFormViewModel
        {
            ApplicationId = application.Id,
            ApplicantName = application.Applicant.FullName,
            UnitLabel = $"{application.Unit.Property.Name} · Unit {application.Unit.UnitNumber}"
        };

        if (!ApplicationRules.CanReview(application.Status))
        {
            ModelState.AddModelError(string.Empty, "Only submitted applications can be reviewed.");
            return ModalForm("_ReviewForm", reviewModel);
        }

        return ModalForm("_ReviewForm", reviewModel);
    }

    [Authorize(Roles = Roles.PropertyManager)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(ReviewFormViewModel model)
    {
        var application = await LoadApplicationAsync(model.ApplicationId);
        if (application is null)
        {
            return NotFound();
        }

        model.ApplicantName = application.Applicant.FullName;
        model.UnitLabel = $"{application.Unit.Property.Name} · Unit {application.Unit.UnitNumber}";

        if (!ModelState.IsValid)
        {
            return ModalForm("_ReviewForm", model);
        }

        var leases = await _db.Leases.Where(l => l.UnitId == application.UnitId).ToListAsync();
        var result = _workflow.Review(
            application,
            model.Outcome,
            model.Comment,
            _userManager.GetUserId(User)!,
            leases);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return ModalForm("_ReviewForm", model);
        }

        await _db.SaveChangesAsync();
        Response.Headers["X-Modal-Result"] = "success";
        Response.Headers["X-Refresh-Target"] = "#application-page";
        return PartialView("_ApplicationPage", ToWizard(application, WizardSection.Summary));
    }

    private async Task<RentalApplication?> LoadApplicationAsync(int id)
    {
        return await _db.Applications
            .Include(a => a.Unit)
            .ThenInclude(u => u.Property)
            .Include(a => a.Applicant)
            .Include(a => a.Residences)
            .Include(a => a.StatusChanges)
            .ThenInclude(c => c.ChangedBy)
            .Include(a => a.Lease)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    private bool CanView(RentalApplication application) =>
        User.IsInRole(Roles.PropertyManager) || application.ApplicantUserId == _userManager.GetUserId(User);

    private bool CanApplicantEdit(RentalApplication application) =>
        User.IsInRole(Roles.Applicant)
        && application.ApplicantUserId == _userManager.GetUserId(User)
        && ApplicationRules.CanEdit(application.Status);

    private ApplicationWizardViewModel ToWizard(RentalApplication application, WizardSection section)
    {
        var canEdit = CanApplicantEdit(application);
        return new ApplicationWizardViewModel
        {
            Id = application.Id,
            UnitId = application.UnitId,
            PropertyName = application.Unit.Property.Name,
            UnitNumber = application.Unit.UnitNumber,
            Status = application.Status,
            CanEdit = canEdit,
            CanWithdraw = User.IsInRole(Roles.Applicant)
                          && application.ApplicantUserId == _userManager.GetUserId(User)
                          && ApplicationRules.CanWithdraw(application.Status),
            CanReview = User.IsInRole(Roles.PropertyManager) && ApplicationRules.CanReview(application.Status),
            ApplicantInfoSaved = application.ApplicantInfoSaved,
            ResidenceHistorySaved = application.ResidenceHistorySaved,
            CurrentSection = section,
            FullName = application.FullName,
            Phone = application.Phone,
            Email = application.Email,
            CurrentAddress = application.CurrentAddress,
            Residences = application.Residences
                .OrderBy(r => r.MoveInDate)
                .Select(r => new ResidenceRowViewModel
                {
                    Id = r.Id,
                    Address = r.Address,
                    LandlordName = r.LandlordName,
                    LandlordPhone = r.LandlordPhone,
                    MoveInDate = r.MoveInDate,
                    MoveOutDate = r.MoveOutDate
                })
                .ToList()
        };
    }

    private ApplicationWizardViewModel ReloadWizard(RentalApplication application, ApplicationWizardViewModel posted)
    {
        var vm = ToWizard(application, posted.CurrentSection);
        vm.FullName = posted.FullName;
        vm.Phone = posted.Phone;
        vm.Email = posted.Email;
        vm.CurrentAddress = posted.CurrentAddress;
        return vm;
    }
}
