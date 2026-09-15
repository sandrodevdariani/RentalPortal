using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Data;
using RentalPortal.Domain;

namespace RentalPortal.ViewComponents;

public class PendingReviewsViewComponent : ViewComponent
{
    private readonly AppDbContext _db;

    public PendingReviewsViewComponent(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var count = await _db.Applications.CountAsync(a => a.Status == ApplicationStatus.Submitted);
        return View(count);
    }
}
