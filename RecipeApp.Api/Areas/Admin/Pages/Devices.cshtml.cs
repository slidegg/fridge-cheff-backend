using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RecipeApp.Api.Data;

namespace RecipeApp.Api.Areas.Admin.Pages;

[Authorize(AuthenticationSchemes = "AdminCookie")]
public class DevicesModel(AppDbContext db) : PageModel
{
    public record DeviceRow(string DeviceId, DateTime CreatedAtUtc, DateTime LastSeenAtUtc, int ScanCount, int SearchCount);
    public List<DeviceRow> Devices { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Devices = await db.DeviceUsers
            .OrderByDescending(u => u.LastSeenAtUtc)
            .Select(u => new DeviceRow(
                u.DeviceId,
                u.CreatedAtUtc,
                u.LastSeenAtUtc,
                u.Scans.Count,
                u.RecipeSearches.Count))
            .ToListAsync();
    }
}
