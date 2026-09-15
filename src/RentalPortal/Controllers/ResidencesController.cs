using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Data;
using RentalPortal.Domain;
using RentalPortal.Entities;
using RentalPortal.Infrastructure;
using RentalPortal.ViewModels;

namespace RentalPortal.Controllers;

[Authorize(Roles = Roles.Applicant)]
public class ResidencesController : ModalController
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ResidencesController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> CreateModal(int applicationId)
    {
        var application = await FindApplicationAsync(applicationId);
        if (application is null)
        {
            return NotFound();
        }

        if (!IsEditableOwner(application))
        {
            return Forbid();
        }

        return ModalForm("_ResidenceForm", new ResidenceFormViewModel
        {
            ApplicationId = applicationId,
            MoveInDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-2))
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ResidenceFormViewModel model)
    {
        var application = await FindApplicationAsync(model.ApplicationId);
        if (application is null)
        {
            return NotFound();
        }

        if (!IsEditableOwner(application))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return ModalForm("_ResidenceForm", model);
        }

        application.Residences.Add(new Residence
        {
            Address = model.Address.Trim(),
            LandlordName = model.LandlordName.Trim(),
            LandlordPhone = model.LandlordPhone.Trim(),
            MoveInDate = model.MoveInDate,
            MoveOutDate = model.MoveOutDate
        });
        await _db.SaveChangesAsync();
        return ModalSuccess("#residence-list", "_ResidenceList", ListModel(application));
    }

    [HttpGet]
    public async Task<IActionResult> EditModal(int id)
    {
        var residence = await _db.Residences.Include(r => r.Application).FirstOrDefaultAsync(r => r.Id == id);
        if (residence is null)
        {
            return NotFound();
        }

        if (!IsEditableOwner(residence.Application))
        {
            return Forbid();
        }

        return ModalForm("_ResidenceForm", new ResidenceFormViewModel
        {
            Id = residence.Id,
            ApplicationId = residence.ApplicationId,
            Address = residence.Address,
            LandlordName = residence.LandlordName,
            LandlordPhone = residence.LandlordPhone,
            MoveInDate = residence.MoveInDate,
            MoveOutDate = residence.MoveOutDate
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ResidenceFormViewModel model)
    {
        if (!model.Id.HasValue)
        {
            return NotFound();
        }

        var residence = await _db.Residences.Include(r => r.Application).ThenInclude(a => a.Residences)
            .FirstOrDefaultAsync(r => r.Id == model.Id.Value);
        if (residence is null)
        {
            return NotFound();
        }

        if (!IsEditableOwner(residence.Application))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return ModalForm("_ResidenceForm", model);
        }

        residence.Address = model.Address.Trim();
        residence.LandlordName = model.LandlordName.Trim();
        residence.LandlordPhone = model.LandlordPhone.Trim();
        residence.MoveInDate = model.MoveInDate;
        residence.MoveOutDate = model.MoveOutDate;
        await _db.SaveChangesAsync();
        return ModalSuccess("#residence-list", "_ResidenceList", ListModel(residence.Application));
    }

    [HttpGet]
    public async Task<IActionResult> DeleteModal(int id)
    {
        var residence = await _db.Residences.Include(r => r.Application).FirstOrDefaultAsync(r => r.Id == id);
        if (residence is null)
        {
            return NotFound();
        }

        if (!IsEditableOwner(residence.Application))
        {
            return Forbid();
        }

        return ModalForm("_DeleteResidence", new ConfirmDeleteViewModel
        {
            Id = residence.Id,
            Title = "Remove residence",
            Message = $"Remove {residence.Address} from this application?",
            Action = nameof(Delete),
            Controller = "Residences"
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var residence = await _db.Residences.Include(r => r.Application).ThenInclude(a => a.Residences)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (residence is null)
        {
            return NotFound();
        }

        if (!IsEditableOwner(residence.Application))
        {
            return Forbid();
        }

        var application = residence.Application;
        application.Residences.Remove(residence);
        _db.Residences.Remove(residence);
        await _db.SaveChangesAsync();
        return ModalSuccess("#residence-list", "_ResidenceList", ListModel(application));
    }

    private Task<RentalApplication?> FindApplicationAsync(int applicationId) =>
        _db.Applications.Include(a => a.Residences).FirstOrDefaultAsync(a => a.Id == applicationId);

    private bool IsEditableOwner(RentalApplication application) =>
        application.ApplicantUserId == _userManager.GetUserId(User)
        && ApplicationRules.CanEdit(application.Status);

    private static ResidenceListViewModel ListModel(RentalApplication application) => new()
    {
        ApplicationId = application.Id,
        CanEdit = true,
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
