using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RecipeApp.Api.Data;
using RecipeApp.Api.Data.Entities;
using RecipeApp.Api.Exceptions;
using RecipeApp.Api.Options;

namespace RecipeApp.Api.Services.Spoonacular;

public class SpoonacularService(
    IHttpClientFactory httpClientFactory,
    IOptions<SpoonacularOptions> options,
    AppDbContext db,
    ILogger<SpoonacularService> logger) : ISpoonacularService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<List<SpoonacularRecipeSummary>> FindByIngredientsAsync(
        IEnumerable<string> ingredients, int number, int ranking, bool ignorePantry)
    {
        var client = httpClientFactory.CreateClient("spoonacular");
        var apiKey = options.Value.ApiKey;
        var ingredientList = string.Join(",", ingredients);
        var url = $"/recipes/findByIngredients?ingredients={Uri.EscapeDataString(ingredientList)}" +
                  $"&number={number}&ranking={ranking}&ignorePantry={ignorePantry}&apiKey={apiKey}";

        var sw = System.Diagnostics.Stopwatch.StartNew();
        int? statusCode = null;
        string? errorMessage = null;

        try
        {
            var response = await client.GetAsync(url);
            statusCode = (int)response.StatusCode;

            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                errorMessage = $"Spoonacular returned {statusCode}: {body}";
                throw MapToFriendlyException(statusCode.Value);
            }

            var raw = JsonSerializer.Deserialize<List<SpoonacularFindByIngredientsItem>>(body, JsonOptions)
                      ?? [];

            return raw.Select(MapToSummary).ToList();
        }
        catch (Exception ex)
        {
            errorMessage ??= ex.Message;
            logger.LogError(ex, "Spoonacular FindByIngredients failed");
            throw;
        }
        finally
        {
            sw.Stop();
            await LogCallAsync(null, "/recipes/findByIngredients", "FindByIngredients",
                statusCode, errorMessage, sw.ElapsedMilliseconds);
        }
    }

    public async Task<SpoonacularRecipeDetail?> GetRecipeDetailAsync(int recipeId)
    {
        var client = httpClientFactory.CreateClient("spoonacular");
        var apiKey = options.Value.ApiKey;
        var url = $"/recipes/{recipeId}/information?includeNutrition=true&apiKey={apiKey}";

        var sw = System.Diagnostics.Stopwatch.StartNew();
        int? statusCode = null;
        string? errorMessage = null;

        try
        {
            var response = await client.GetAsync(url);
            statusCode = (int)response.StatusCode;

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                errorMessage = $"Spoonacular returned {statusCode}: {body}";
                throw MapToFriendlyException(statusCode.Value);
            }

            var raw = JsonSerializer.Deserialize<SpoonacularRecipeInfoResponse>(body, JsonOptions)
                      ?? throw new ExternalServiceException("Our recipe service returned an unexpected response. Please try again later.");

            return MapToDetail(raw);
        }
        catch (Exception ex)
        {
            errorMessage ??= ex.Message;
            logger.LogError(ex, "Spoonacular GetRecipeDetail failed for {RecipeId}", recipeId);
            throw;
        }
        finally
        {
            sw.Stop();
            await LogCallAsync(null, $"/recipes/{recipeId}/information", "GetRecipeDetail",
                statusCode, errorMessage, sw.ElapsedMilliseconds);
        }
    }

    private static ExternalServiceException MapToFriendlyException(int statusCode) => statusCode switch
    {
        402 => new ExternalServiceException("Our recipe service has reached its daily usage limit. Please try again later."),
        429 => new ExternalServiceException("Our recipe service is temporarily busy. Please try again in a moment."),
        >= 500 => new ExternalServiceException("Our recipe service is temporarily unavailable. Please try again later."),
        _ => new ExternalServiceException("Unable to reach the recipe service right now. Please try again later."),
    };

    private async Task LogCallAsync(Guid? deviceUserId, string endpoint, string requestType,
        int? statusCode, string? errorMessage, long durationMs)
    {
        db.ExternalApiCallLogs.Add(new ExternalApiCallLog
        {
            Id = Guid.NewGuid(),
            DeviceUserId = deviceUserId,
            Provider = ApiProvider.Spoonacular,
            Endpoint = endpoint,
            RequestType = requestType,
            StatusCode = statusCode,
            Success = errorMessage == null,
            ErrorMessage = errorMessage,
            DurationMs = durationMs,
            CreatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    private static SpoonacularRecipeSummary MapToSummary(SpoonacularFindByIngredientsItem item)
    {
        var used = item.UsedIngredients?.Select(MapIngredient).Where(IsRealIngredient).ToList() ?? [];
        var missed = item.MissedIngredients?.Select(MapIngredient).Where(IsRealIngredient).ToList() ?? [];

        return new SpoonacularRecipeSummary(
            Id: item.Id,
            Title: item.Title ?? string.Empty,
            Image: item.Image ?? string.Empty,
            UsedIngredientCount: used.Count,
            MissedIngredientCount: missed.Count,
            UsedIngredients: used,
            MissedIngredients: missed,
            Likes: item.Likes
        );
    }

    private static SpoonacularIngredient MapIngredient(SpoonacularRawIngredient i) =>
        new(i.Id, i.Name ?? string.Empty, i.Amount, i.Unit ?? string.Empty);

    /// <summary>Spoonacular occasionally leaks recipe metadata (e.g. "preparation time: minutes")
    /// into the ingredients list as if it were a real ingredient. Real ingredient names never
    /// contain a colon, so this is a reliable, low-risk filter.</summary>
    private static bool IsRealIngredient(SpoonacularIngredient i) =>
        !string.IsNullOrWhiteSpace(i.Name) && !i.Name.Contains(':');

    private static bool IsRealIngredient(SpoonacularExtendedIngredient i) =>
        !string.IsNullOrWhiteSpace(i.Name) && !i.Name.Contains(':');

    private static SpoonacularRecipeDetail MapToDetail(SpoonacularRecipeInfoResponse r)
    {
        // Spoonacular splits instructions into groups (e.g. "For the sauce", "For the filling"),
        // each restarting at step 1 — renumber sequentially so step numbers stay unique across groups.
        var steps = r.AnalyzedInstructions?
            .SelectMany(i => i.Steps ?? [])
            .Select((s, index) => new SpoonacularStep(index + 1, s.Step ?? string.Empty))
            .ToList() ?? [];

        var ingredients = r.ExtendedIngredients?
            .Select(i => new SpoonacularExtendedIngredient(
                i.Name ?? string.Empty, i.Amount, i.Unit ?? string.Empty))
            .Where(IsRealIngredient)
            .ToList() ?? [];

        SpoonacularNutrition? nutrition = null;
        if (r.Nutrition?.Nutrients?.Count > 0)
        {
            var nutrients = r.Nutrition.Nutrients
                .Select(n => new SpoonacularNutrient(n.Name ?? string.Empty, n.Amount, n.Unit ?? string.Empty))
                .ToList();
            nutrition = new SpoonacularNutrition(nutrients);
        }

        return new SpoonacularRecipeDetail(
            Id: r.Id,
            Title: r.Title ?? string.Empty,
            Image: r.Image ?? string.Empty,
            Servings: r.Servings,
            ReadyInMinutes: r.ReadyInMinutes,
            SourceUrl: r.SourceUrl,
            ExtendedIngredients: ingredients,
            Steps: steps,
            Nutrition: nutrition
        );
    }
}

// Raw JSON shapes are defined in SpoonacularRawModels.cs
