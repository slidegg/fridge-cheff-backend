using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RecipeApp.Api.Data;

namespace RecipeApp.Api.Areas.Admin.Pages;

[Authorize(AuthenticationSchemes = "AdminCookie")]
public class ApiCallsModel(AppDbContext db) : PageModel
{
    public record ApiCallRow(string Provider, string Endpoint, string RequestType,
        int? StatusCode, bool Success, long DurationMs, DateTime CreatedAtUtc, string? ErrorMessage);

    public List<ApiCallRow> Calls { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Calls = await db.ExternalApiCallLogs
            .OrderByDescending(l => l.CreatedAtUtc)
            .Take(200)
            .Select(l => new ApiCallRow(
                l.Provider.ToString(),
                l.Endpoint,
                l.RequestType,
                l.StatusCode,
                l.Success,
                l.DurationMs,
                l.CreatedAtUtc,
                l.ErrorMessage))
            .ToListAsync();
    }
}
