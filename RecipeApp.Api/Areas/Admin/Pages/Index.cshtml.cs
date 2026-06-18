using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RecipeApp.Api.Data;

namespace RecipeApp.Api.Areas.Admin.Pages;

[Authorize(AuthenticationSchemes = "AdminCookie")]
public class IndexModel(AppDbContext db) : PageModel
{
    public string Today { get; private set; } = string.Empty;
    public int ScansToday { get; private set; }
    public int SearchesToday { get; private set; }
    public int ApiCallsToday { get; private set; }
    public int FailedApiCallsToday { get; private set; }

    public record ScanRow(string DeviceId, string Status, int ImageCount, DateTime CreatedAtUtc);
    public record DeviceRow(string DeviceId, DateTime LastSeenAtUtc);

    public List<ScanRow> RecentScans { get; private set; } = [];
    public List<DeviceRow> RecentDevices { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var todayUtc = DateTime.UtcNow.Date;
        Today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        ScansToday = await db.FridgeScans.CountAsync(s => s.CreatedAtUtc >= todayUtc);
        SearchesToday = await db.RecipeSearches.CountAsync(s => s.CreatedAtUtc >= todayUtc);
        ApiCallsToday = await db.ExternalApiCallLogs.CountAsync(l => l.CreatedAtUtc >= todayUtc);
        FailedApiCallsToday = await db.ExternalApiCallLogs
            .CountAsync(l => l.CreatedAtUtc >= todayUtc && !l.Success);

        RecentScans = await db.FridgeScans
            .OrderByDescending(s => s.CreatedAtUtc)
            .Take(10)
            .Select(s => new ScanRow(s.DeviceUser.DeviceId, s.Status.ToString(), s.ImageCount, s.CreatedAtUtc))
            .ToListAsync();

        RecentDevices = await db.DeviceUsers
            .OrderByDescending(u => u.LastSeenAtUtc)
            .Take(10)
            .Select(u => new DeviceRow(u.DeviceId, u.LastSeenAtUtc))
            .ToListAsync();

        return Page();
    }
}
