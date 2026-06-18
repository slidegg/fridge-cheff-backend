using RecipeApp.Api.Exceptions;

namespace RecipeApp.Api.Middleware;

public class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (RateLimitException ex)
        {
            logger.LogWarning("Rate limit: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await WriteErrorAsync(context, ex.Message);
        }
        catch (NotFoundException ex)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await WriteErrorAsync(context, ex.Message);
        }
        catch (AppValidationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteErrorAsync(context, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await WriteErrorAsync(context, "An unexpected error occurred. Please try again.");
        }
    }

    private static Task WriteErrorAsync(HttpContext context, string message)
    {
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(new { error = message });
    }
}
