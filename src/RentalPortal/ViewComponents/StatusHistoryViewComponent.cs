using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Data;
using RentalPortal.ViewModels;

namespace RentalPortal.ViewComponents;

public class StatusHistoryViewComponent : ViewComponent
{
    private readonly AppDbContext _db;

    public StatusHistoryViewComponent(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync(int applicationId)
    {
        var items = await _db.StatusChanges
            .AsNoTracking()
            .Where(c => c.ApplicationId == applicationId)
            .OrderBy(c => c.ChangedAt)
            .Select(c => new StatusHistoryItemViewModel
            {
                Actor = c.ChangedBy.FirstName + " " + c.ChangedBy.LastName,
                ChangedAt = c.ChangedAt,
                Description = c.Outcome.HasValue
                    ? $"{c.ToStatus} ({c.Outcome})"
                    : c.FromStatus.HasValue
                        ? $"{c.FromStatus} → {c.ToStatus}"
                        : c.ToStatus.ToString(),
                Comment = c.Comment
            })
            .ToListAsync();

        return View(items);
    }
}
