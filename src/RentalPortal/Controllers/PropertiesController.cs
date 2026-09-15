using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Data;
using RentalPortal.Domain;
using RentalPortal.Entities;
using RentalPortal.Infrastructure;
using RentalPortal.ViewModels;

namespace RentalPortal.Controllers;

[Authorize(Roles = Roles.PropertyManager)]
public class PropertiesController : ModalController
{
    private readonly AppDbContext _db;

    public PropertiesController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        return View(await LoadListAsync());
    }

    [HttpGet]
    public IActionResult CreateModal()
    {
        return ModalForm("_PropertyForm", new PropertyFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PropertyFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return ModalForm("_PropertyForm", model);
        }

        _db.Properties.Add(new Property
        {
            Name = model.Name.Trim(),
            Address = model.Address.Trim(),
            City = model.City.Trim(),
            State = model.State.Trim(),
            ZipCode = model.ZipCode.Trim()
        });
        await _db.SaveChangesAsync();
        return ModalSuccess("#property-list", "_PropertyList", await LoadListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> EditModal(int id)
    {
        var property = await _db.Properties.FindAsync(id);
        if (property is null)
        {
            return NotFound();
        }

        return ModalForm("_PropertyForm", ToForm(property));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PropertyFormViewModel model)
    {
        if (!model.Id.HasValue)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return ModalForm("_PropertyForm", model);
        }

        var property = await _db.Properties.FindAsync(model.Id.Value);
        if (property is null)
        {
            return NotFound();
        }

        property.Name = model.Name.Trim();
        property.Address = model.Address.Trim();
        property.City = model.City.Trim();
        property.State = model.State.Trim();
        property.ZipCode = model.ZipCode.Trim();
        await _db.SaveChangesAsync();
        return ModalSuccess("#property-list", "_PropertyList", await LoadListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> DeleteModal(int id)
    {
        var property = await _db.Properties.FindAsync(id);
        if (property is null)
        {
            return NotFound();
        }

        return ModalForm("_DeleteProperty", new ConfirmDeleteViewModel
        {
            Id = property.Id,
            Title = "Remove property",
            Message = $"Remove {property.Name}? Units without applications or leases will be removed with it.",
            Action = nameof(Delete),
            Controller = "Properties"
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var property = await _db.Properties
            .Include(p => p.Units)
            .ThenInclude(u => u.Applications)
            .Include(p => p.Units)
            .ThenInclude(u => u.Leases)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (property is null)
        {
            return NotFound();
        }

        if (property.Units.Any(u => u.Applications.Count > 0 || u.Leases.Count > 0))
        {
            ModelState.AddModelError(string.Empty, "This property has units with applications or leases and cannot be removed.");
            return ModalForm("_DeleteProperty", new ConfirmDeleteViewModel
            {
                Id = property.Id,
                Title = "Remove property",
                Message = $"Remove {property.Name}?",
                Action = nameof(Delete),
                Controller = "Properties"
            });
        }

        _db.Properties.Remove(property);
        await _db.SaveChangesAsync();
        return ModalSuccess("#property-list", "_PropertyList", await LoadListAsync());
    }

    private async Task<List<PropertyListItemViewModel>> LoadListAsync()
    {
        return await _db.Properties
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new PropertyListItemViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Address = p.Address,
                City = p.City,
                State = p.State,
                ZipCode = p.ZipCode,
                UnitCount = p.Units.Count
            })
            .ToListAsync();
    }

    private static PropertyFormViewModel ToForm(Property property) => new()
    {
        Id = property.Id,
        Name = property.Name,
        Address = property.Address,
        City = property.City,
        State = property.State,
        ZipCode = property.ZipCode
    };
}
