using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RecipeApp.Api.Data;

namespace RecipeApp.Api.Areas.Admin.Pages;

[Authorize(AuthenticationSchemes = "AdminCookie")]
public class UsageModel(AppDbContext db) : PageModel
{
    public record UsageRow(string DeviceId, string DateUtc, int ScansUsed,
        int RecipeSearchesUsed, int RecipeDetailsUsed, DateTime UpdatedAtUtc);

    public string Today { get; private set; } = string.Empty;
    public List<UsageRow> Counters { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        Counters = await db.UsageCounters
            .OrderByDescending(u => u.DateUtc)
            .ThenByDescending(u => u.UpdatedAtUtc)
            .Take(100)
            .Select(u => new UsageRow(
                u.DeviceUser.DeviceId,
                u.DateUtc.ToString(),
                u.ScansUsed,
                u.RecipeSearchesUsed,
                u.RecipeDetailsUsed,
                u.UpdatedAtUtc))
            .ToListAsync();
    }
}
