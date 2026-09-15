using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Data;
using RentalPortal.Domain;
using RentalPortal.ViewModels;

namespace RentalPortal.ViewComponents;

public class PropertyUnitsViewComponent : ViewComponent
{
    private readonly AppDbContext _db;
    private readonly IClock _clock;

    public PropertyUnitsViewComponent(AppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IViewComponentResult> InvokeAsync(int propertyId)
    {
        var today = _clock.Today;
        var units = await _db.Units
            .AsNoTracking()
            .Include(u => u.UnitType)
            .Where(u => u.PropertyId == propertyId)
            .OrderBy(u => u.UnitNumber)
            .Select(u => new PropertyUnitRowViewModel
            {
                Id = u.Id,
                PropertyId = u.PropertyId,
                UnitNumber = u.UnitNumber,
                Bedrooms = u.Bedrooms,
                MonthlyRent = u.MonthlyRent,
                UnitTypeName = u.UnitType.Name,
                UnitTypeIsActive = u.UnitType.IsActive,
                IsAvailable = !u.Leases.Any(l => l.StartDate <= today && today < l.EndDate)
            })
            .ToListAsync();

        return View(new PropertyUnitsViewModel
        {
            PropertyId = propertyId,
            Units = units
        });
    }
}
