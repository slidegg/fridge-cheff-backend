using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RecipeApp.Api.Data;

namespace RecipeApp.Api.Areas.Admin.Pages;

[Authorize(AuthenticationSchemes = "AdminCookie")]
public class ScansModel(AppDbContext db) : PageModel
{
    public record ScanRow(Guid ScanId, string DeviceId, string Status, int ImageCount,
        int DetectedCount, int ConfirmedCount, DateTime CreatedAtUtc, string? ErrorMessage);

    public List<ScanRow> Scans { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Scans = await db.FridgeScans
            .OrderByDescending(s => s.CreatedAtUtc)
            .Take(100)
            .Select(s => new ScanRow(
                s.Id,
                s.DeviceUser.DeviceId,
                s.Status.ToString(),
                s.ImageCount,
                s.DetectedIngredients.Count,
                s.ConfirmedIngredients.Count,
                s.CreatedAtUtc,
                s.ErrorMessage))
            .ToListAsync();
    }
}
