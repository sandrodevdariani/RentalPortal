using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Data;
using RentalPortal.Domain;
using RentalPortal.Entities;
using RentalPortal.Infrastructure;
using RentalPortal.ViewModels;

namespace RentalPortal.Controllers;

public class UnitsController : ModalController
{
    private readonly AppDbContext _db;
    private readonly IClock _clock;

    public UnitsController(AppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    [Authorize(Roles = Roles.Applicant)]
    public async Task<IActionResult> Available()
    {
        var today = _clock.Today;
        var units = await _db.Units
            .AsNoTracking()
            .Include(u => u.Property)
            .Include(u => u.UnitType)
            .Where(u => !u.Leases.Any(l => l.StartDate <= today && today < l.EndDate))
            .OrderBy(u => u.Property.Name)
            .ThenBy(u => u.UnitNumber)
            .Select(u => new AvailableUnitViewModel
            {
                UnitId = u.Id,
                PropertyName = u.Property.Name,
                Address = u.Property.Address + ", " + u.Property.City + " " + u.Property.State,
                UnitNumber = u.UnitNumber,
                Bedrooms = u.Bedrooms,
                MonthlyRent = u.MonthlyRent,
                UnitType = u.UnitType.Name
            })
            .ToListAsync();

        return View(units);
    }

    [Authorize(Roles = Roles.PropertyManager)]
    [HttpGet]
    public async Task<IActionResult> CreateModal(int propertyId)
    {
        var property = await _db.Properties.FindAsync(propertyId);
        if (property is null)
        {
            return NotFound();
        }

        var model = new UnitFormViewModel
        {
            PropertyId = propertyId,
            PropertyName = property.Name,
            UnitTypes = await UnitTypeOptionsAsync()
        };
        return ModalForm("_UnitForm", model);
    }

    [Authorize(Roles = Roles.PropertyManager)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UnitFormViewModel model)
    {
        await ValidateUnitTypeAsync(model.UnitTypeId, currentUnitTypeId: null);
        await ValidateUniqueNumberAsync(model);
        model.UnitTypes = await UnitTypeOptionsAsync();
        model.PropertyName = await PropertyNameAsync(model.PropertyId);

        if (!ModelState.IsValid)
        {
            return ModalForm("_UnitForm", model);
        }

        _db.Units.Add(new Unit
        {
            PropertyId = model.PropertyId,
            UnitNumber = model.UnitNumber.Trim(),
            Bedrooms = model.Bedrooms,
            MonthlyRent = model.MonthlyRent,
            UnitTypeId = model.UnitTypeId
        });
        await _db.SaveChangesAsync();
        return ModalSuccessComponent($"#units-for-{model.PropertyId}", "PropertyUnits", new { propertyId = model.PropertyId });
    }

    [Authorize(Roles = Roles.PropertyManager)]
    [HttpGet]
    public async Task<IActionResult> EditModal(int id)
    {
        var unit = await _db.Units.Include(u => u.Property).FirstOrDefaultAsync(u => u.Id == id);
        if (unit is null)
        {
            return NotFound();
        }

        var model = new UnitFormViewModel
        {
            Id = unit.Id,
            PropertyId = unit.PropertyId,
            PropertyName = unit.Property.Name,
            UnitNumber = unit.UnitNumber,
            Bedrooms = unit.Bedrooms,
            MonthlyRent = unit.MonthlyRent,
            UnitTypeId = unit.UnitTypeId,
            UnitTypes = await UnitTypeOptionsAsync(unit.UnitTypeId)
        };
        return ModalForm("_UnitForm", model);
    }

    [Authorize(Roles = Roles.PropertyManager)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UnitFormViewModel model)
    {
        if (!model.Id.HasValue)
        {
            return NotFound();
        }

        var unit = await _db.Units.FindAsync(model.Id.Value);
        if (unit is null)
        {
            return NotFound();
        }

        await ValidateUnitTypeAsync(model.UnitTypeId, unit.UnitTypeId);
        await ValidateUniqueNumberAsync(model, unit.Id);
        model.UnitTypes = await UnitTypeOptionsAsync(unit.UnitTypeId);
        model.PropertyName = await PropertyNameAsync(model.PropertyId);

        if (!ModelState.IsValid)
        {
            return ModalForm("_UnitForm", model);
        }

        unit.UnitNumber = model.UnitNumber.Trim();
        unit.Bedrooms = model.Bedrooms;
        unit.MonthlyRent = model.MonthlyRent;
        unit.UnitTypeId = model.UnitTypeId;
        await _db.SaveChangesAsync();
        return ModalSuccessComponent($"#units-for-{unit.PropertyId}", "PropertyUnits", new { propertyId = unit.PropertyId });
    }

    [Authorize(Roles = Roles.PropertyManager)]
    [HttpGet]
    public async Task<IActionResult> DeleteModal(int id)
    {
        var unit = await _db.Units.FindAsync(id);
        if (unit is null)
        {
            return NotFound();
        }

        return ModalForm("_DeleteUnit", new ConfirmDeleteViewModel
        {
            Id = unit.Id,
            PropertyId = unit.PropertyId,
            Title = "Remove unit",
            Message = $"Remove unit {unit.UnitNumber}?",
            Action = nameof(Delete),
            Controller = "Units"
        });
    }

    [Authorize(Roles = Roles.PropertyManager)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var unit = await _db.Units
            .Include(u => u.Applications)
            .Include(u => u.Leases)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (unit is null)
        {
            return NotFound();
        }

        var propertyId = unit.PropertyId;
        if (unit.Applications.Count > 0 || unit.Leases.Count > 0)
        {
            ModelState.AddModelError(string.Empty, "This unit has applications or leases and cannot be removed.");
            return ModalForm("_DeleteUnit", new ConfirmDeleteViewModel
            {
                Id = unit.Id,
                PropertyId = propertyId,
                Title = "Remove unit",
                Message = $"Remove unit {unit.UnitNumber}?",
                Action = nameof(Delete),
                Controller = "Units"
            });
        }

        _db.Units.Remove(unit);
        await _db.SaveChangesAsync();
        return ModalSuccessComponent($"#units-for-{propertyId}", "PropertyUnits", new { propertyId });
    }

    private async Task ValidateUnitTypeAsync(int unitTypeId, int? currentUnitTypeId)
    {
        var type = await _db.UnitTypes.FindAsync(unitTypeId);
        if (type is null)
        {
            ModelState.AddModelError(nameof(UnitFormViewModel.UnitTypeId), "Select a unit type.");
            return;
        }

        if (!UnitTypeRules.CanAssign(type.IsActive, type.Id, currentUnitTypeId))
        {
            ModelState.AddModelError(nameof(UnitFormViewModel.UnitTypeId), "This unit type is inactive and cannot be selected.");
        }
    }

    private async Task ValidateUniqueNumberAsync(UnitFormViewModel model, int? excludeId = null)
    {
        var number = model.UnitNumber?.Trim() ?? string.Empty;
        var exists = await _db.Units.AnyAsync(u =>
            u.PropertyId == model.PropertyId &&
            u.UnitNumber == number &&
            u.Id != (excludeId ?? 0));
        if (exists)
        {
            ModelState.AddModelError(nameof(model.UnitNumber), "A unit with this number already exists at the property.");
        }
    }

    private async Task<IEnumerable<SelectListItem>> UnitTypeOptionsAsync(int? currentId = null)
    {
        return await _db.UnitTypes
            .AsNoTracking()
            .Where(t => t.IsActive || t.Id == currentId)
            .OrderBy(t => t.Name)
            .Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = t.IsActive ? t.Name : t.Name + " (inactive)"
            })
            .ToListAsync();
    }

    private async Task<string?> PropertyNameAsync(int propertyId)
    {
        return await _db.Properties.Where(p => p.Id == propertyId).Select(p => p.Name).FirstOrDefaultAsync();
    }
}
