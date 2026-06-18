namespace RecipeApp.Api.Middleware;

/// <summary>
/// Optional middleware placeholder for future JWT-based device resolution.
/// Device lookup is currently handled per-request in each service via DeviceService.
/// </summary>
public class DeviceUserMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context) => next(context);
}
