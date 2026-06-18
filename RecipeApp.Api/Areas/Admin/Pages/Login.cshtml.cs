using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RecipeApp.Api.Areas.Admin.Pages;

public class LoginModel(IConfiguration config) : PageModel
{
    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; private set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Index", new { area = "Admin" });

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var adminPassword = config["Admin:Password"];

        if (string.IsNullOrEmpty(adminPassword) || Password != adminPassword)
        {
            ErrorMessage = "Invalid password.";
            return Page();
        }

        var claims = new List<Claim> { new(ClaimTypes.Name, "admin") };
        var identity = new ClaimsIdentity(claims, "AdminCookie");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("AdminCookie", principal);

        return RedirectToPage("/Index", new { area = "Admin" });
    }
}
