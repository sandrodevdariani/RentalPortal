using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Data;
using RentalPortal.Domain;
using RentalPortal.Entities;
using RentalPortal.Models;
using RentalPortal.ViewModels;

namespace RentalPortal.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly IClock _clock;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(AppDbContext db, IClock clock, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _clock = clock;
        _userManager = userManager;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var today = _clock.Today;
        if (User.Identity?.IsAuthenticated != true)
        {
            var featured = await _db.Units
                .AsNoTracking()
                .Include(u => u.Property)
                .Include(u => u.UnitType)
                .Where(u => !u.Leases.Any(l => l.StartDate <= today && today < l.EndDate))
                .OrderBy(u => u.Property.Name)
                .ThenBy(u => u.UnitNumber)
                .Take(3)
                .Select(u => new AvailableUnitViewModel
                {
                    UnitId = u.Id,
                    PropertyName = u.Property.Name,
                    Address = u.Property.Address + ", " + u.Property.City,
                    UnitNumber = u.UnitNumber,
                    Bedrooms = u.Bedrooms,
                    MonthlyRent = u.MonthlyRent,
                    UnitType = u.UnitType.Name
                })
                .ToListAsync();

            return View("Landing", new LandingViewModel
            {
                Featured = featured,
                PropertyCount = await _db.Properties.CountAsync(),
                AvailableCount = await _db.Units.CountAsync(u => !u.Leases.Any(l => l.StartDate <= today && today < l.EndDate))
            });
        }

        if (User.IsInRole(Roles.PropertyManager))
        {
            var pending = await _db.Applications.CountAsync(a => a.Status == ApplicationStatus.Submitted);
            var properties = await _db.Properties.CountAsync();
            var units = await _db.Units.CountAsync();
            ViewBag.Pending = pending;
            ViewBag.Properties = properties;
            ViewBag.Units = units;
            return View("ManagerHome");
        }

        var userId = _userManager.GetUserId(User)!;
        var available = await _db.Units
            .AsNoTracking()
            .Include(u => u.Property)
            .Include(u => u.UnitType)
            .Where(u => !u.Leases.Any(l => l.StartDate <= today && today < l.EndDate))
            .OrderBy(u => u.Property.Name)
            .ThenBy(u => u.UnitNumber)
            .Take(6)
            .Select(u => new AvailableUnitViewModel
            {
                UnitId = u.Id,
                PropertyName = u.Property.Name,
                Address = u.Property.Address + ", " + u.Property.City,
                UnitNumber = u.UnitNumber,
                Bedrooms = u.Bedrooms,
                MonthlyRent = u.MonthlyRent,
                UnitType = u.UnitType.Name
            })
            .ToListAsync();

        var applications = await _db.Applications
            .AsNoTracking()
            .Where(a => a.ApplicantUserId == userId)
            .OrderByDescending(a => a.UpdatedAt)
            .Take(6)
            .Select(a => new ApplicationListItemViewModel
            {
                Id = a.Id,
                ApplicantName = a.FullName,
                PropertyName = a.Unit.Property.Name,
                UnitNumber = a.Unit.UnitNumber,
                Status = a.Status,
                UpdatedAt = a.UpdatedAt
            })
            .ToListAsync();

        ViewBag.Available = available;
        ViewBag.Applications = applications;
        return View("ApplicantHome");
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
