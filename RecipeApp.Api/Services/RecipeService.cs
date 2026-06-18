using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RecipeApp.Api.Contracts.Requests;
using RecipeApp.Api.Contracts.Responses;
using RecipeApp.Api.Data;
using RecipeApp.Api.Data.Entities;
using RecipeApp.Api.Exceptions;
using RecipeApp.Api.Options;
using RecipeApp.Api.Services.Spoonacular;

namespace RecipeApp.Api.Services;

public class RecipeService(
    AppDbContext db,
    DeviceService deviceService,
    ISpoonacularService spoonacularService,
    UsageLimitService usageLimitService,
    IOptions<FreeTierOptions> freeTierOptions,
    ILogger<RecipeService> logger)
{
    private static readonly HashSet<string> ProteinIngredients = new(StringComparer.OrdinalIgnoreCase)
    {
        "chicken", "chicken breast", "beef", "pork", "turkey", "fish", "salmon", "tuna",
        "shrimp", "eggs", "egg", "tofu", "greek yogurt", "cottage cheese", "lentils", "beans",
    };

    private static readonly HashSet<string> VegetableIngredients = new(StringComparer.OrdinalIgnoreCase)
    {
        "spinach", "broccoli", "kale", "lettuce", "cucumber", "zucchini", "tomato",
        "pepper", "mushroom", "asparagus", "cauliflower", "carrot", "celery", "onion",
        "garlic", "arugula", "cabbage", "green beans", "peas",
    };

    private const string MacroDisclaimer =
        "Nutritional values are estimates provided by Spoonacular and may vary based on ingredient brands, preparation methods, and portion sizes.";

    public async Task<SuggestRecipesResponse> SuggestAsync(SuggestRecipesRequest req)
    {
        var deviceUser = await deviceService.GetByDeviceIdAsync(req.DeviceId);
        var goal = ParseGoal(req.Goal);

        await usageLimitService.CheckAndIncrementAsync(deviceUser.Id, LimitType.RecipeSearch);

        var spoonacularResults = await spoonacularService.FindByIngredientsAsync(
            req.Ingredients,
            number: freeTierOptions.Value.MaxSuggestionsPerSearch * 3,  // fetch more, filter down
            ranking: 2,
            ignorePantry: true
        );

        // Only show recipes where user has all required ingredients
        var feasible = spoonacularResults
            .Where(r => r.MissedIngredientCount == 0)
            .ToList();

        logger.LogInformation("Recipe suggest: {Total} results, {Feasible} feasible after filter",
            spoonacularResults.Count, feasible.Count);

        List<RecipeSummaryDto> recipeDtos;
        EmptyStateDto? emptyState = null;

        if (feasible.Count == 0)
        {
            emptyState = new EmptyStateDto(
                Message: "No recipes found using only your available ingredients.",
                Suggestions:
                [
                    "Allow basic pantry items like salt, pepper, and oil (always available).",
                    "Allow up to 1–2 missing ingredients and pick up a few extras.",
                    "Add more ingredients from your kitchen and search again.",
                ]
            );
            recipeDtos = [];
        }
        else
        {
            var scored = feasible
                .Select(r => (Recipe: r, Score: ComputeScore(r, req.Ingredients, goal)))
                .OrderByDescending(x => x.Score)
                .Take(freeTierOptions.Value.MaxSuggestionsPerSearch)
                .ToList();

            recipeDtos = scored.Select(x => new RecipeSummaryDto(
                Id: x.Recipe.Id,
                Title: x.Recipe.Title,
                ImageUrl: x.Recipe.Image,
                UsedIngredients: x.Recipe.UsedIngredients.Select(i => i.Name).ToList(),
                MissedIngredients: x.Recipe.MissedIngredients.Select(i => i.Name).ToList(),
                UsedIngredientCount: x.Recipe.UsedIngredientCount,
                MissedIngredientCount: x.Recipe.MissedIngredientCount,
                ReadyInMinutes: 0,  // not available at search level
                Score: Math.Round(x.Score, 1),
                GoalReason: GenerateGoalReason(x.Recipe, goal)
            )).ToList();
        }

        db.RecipeSearches.Add(new RecipeSearch
        {
            Id = Guid.NewGuid(),
            DeviceUserId = deviceUser.Id,
            FridgeScanId = req.ScanId,
            Goal = goal,
            IngredientsJson = JsonSerializer.Serialize(req.Ingredients),
            ResultCount = recipeDtos.Count,
            CreatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        return new SuggestRecipesResponse(recipeDtos, emptyState);
    }

    public async Task<RecipeDetailResponse> GetDetailAsync(int recipeId, string deviceId)
    {
        var deviceUser = await deviceService.GetByDeviceIdAsync(deviceId);

        await usageLimitService.CheckAndIncrementAsync(deviceUser.Id, LimitType.RecipeDetail);

        var detail = await spoonacularService.GetRecipeDetailAsync(recipeId)
                     ?? throw new NotFoundException($"Recipe {recipeId} not found.");

        // Get user's most recent confirmed ingredients to compute missing
        var userIngredients = await GetLatestConfirmedIngredientNamesAsync(deviceUser.Id);
        var missingIngredients = ComputeMissingIngredients(detail.ExtendedIngredients, userIngredients);

        // Log the view
        db.RecipeInteractions.Add(new RecipeInteraction
        {
            Id = Guid.NewGuid(),
            DeviceUserId = deviceUser.Id,
            SpoonacularRecipeId = recipeId,
            Action = InteractionAction.Viewed,
            CreatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        return new RecipeDetailResponse(
            Id: detail.Id,
            Title: detail.Title,
            ImageUrl: detail.Image,
            Servings: detail.Servings,
            ReadyInMinutes: detail.ReadyInMinutes,
            SourceUrl: detail.SourceUrl,
            MacrosPerServing: ExtractMacros(detail.Nutrition),
            Ingredients: detail.ExtendedIngredients
                .Select(i => new RecipeIngredientDto(i.Name, i.Amount, i.Unit))
                .ToList(),
            Steps: detail.Steps
                .Select(s => new RecipeStepDto(s.Number, s.Step))
                .ToList(),
            MissingIngredients: missingIngredients,
            MacroDisclaimer: MacroDisclaimer
        );
    }

    public async Task SaveRecipeAsync(int recipeId, SaveRecipeRequest req)
    {
        var deviceUser = await deviceService.GetByDeviceIdAsync(req.DeviceId);

        var alreadySaved = await db.SavedRecipes
            .AnyAsync(s => s.DeviceUserId == deviceUser.Id && s.SpoonacularRecipeId == recipeId);

        if (!alreadySaved)
        {
            db.SavedRecipes.Add(new SavedRecipe
            {
                Id = Guid.NewGuid(),
                DeviceUserId = deviceUser.Id,
                SpoonacularRecipeId = recipeId,
                Title = req.Title,
                ImageUrl = req.ImageUrl,
                CreatedAtUtc = DateTime.UtcNow,
            });

            db.RecipeInteractions.Add(new RecipeInteraction
            {
                Id = Guid.NewGuid(),
                DeviceUserId = deviceUser.Id,
                SpoonacularRecipeId = recipeId,
                Action = InteractionAction.Saved,
                CreatedAtUtc = DateTime.UtcNow,
            });

            await db.SaveChangesAsync();
        }
    }

    private async Task<List<string>> GetLatestConfirmedIngredientNamesAsync(Guid deviceUserId)
    {
        var latestScan = await db.FridgeScans
            .Where(s => s.DeviceUserId == deviceUserId && s.Status == ScanStatus.Processed)
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync();

        if (latestScan is null)
            return [];

        return await db.ConfirmedIngredients
            .Where(c => c.FridgeScanId == latestScan.Id)
            .Select(c => c.NormalizedName)
            .ToListAsync();
    }

    private static List<string> ComputeMissingIngredients(
        List<SpoonacularExtendedIngredient> recipeIngredients,
        List<string> userIngredients)
    {
        // Pantry staples that are never considered "missing"
        var pantryStaples = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "salt", "pepper", "water", "oil", "olive oil", "vegetable oil", "cooking oil",
            "butter", "flour", "sugar", "baking soda", "baking powder",
        };

        return recipeIngredients
            .Where(ri => !pantryStaples.Contains(ri.Name))
            .Where(ri => !userIngredients.Any(u =>
                u.Contains(ri.Name, StringComparison.OrdinalIgnoreCase) ||
                ri.Name.Contains(u, StringComparison.OrdinalIgnoreCase)))
            .Select(ri => ri.Name)
            .ToList();
    }

    private static MacrosDto? ExtractMacros(SpoonacularNutrition? nutrition)
    {
        if (nutrition?.Nutrients is null || nutrition.Nutrients.Count == 0)
            return null;

        double Get(string name) =>
            nutrition.Nutrients
                .FirstOrDefault(n => n.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                ?.Amount ?? 0;

        return new MacrosDto(
            Calories: Get("Calories"),
            ProteinG: Get("Protein"),
            CarbsG: Get("Carbohydrates"),
            FatG: Get("Fat")
        );
    }

    private static double ComputeScore(
        SpoonacularRecipeSummary recipe,
        List<string> userIngredients,
        RecipeGoal goal)
    {
        var coverageScore = userIngredients.Count > 0
            ? (recipe.UsedIngredientCount / (double)userIngredients.Count) * 40.0
            : 20.0;

        var popularityBonus = recipe.Likes > 0 ? Math.Min(recipe.Likes / 10.0, 25.0) : 10.0;

        return goal switch
        {
            RecipeGoal.ProteinFirst =>
                coverageScore * 0.6 +
                popularityBonus * 0.1 +
                recipe.UsedIngredients.Count(i => ProteinIngredients.Contains(i.Name)) * 12.0,

            RecipeGoal.LowCalories =>
                coverageScore * 0.6 +
                popularityBonus * 0.1 +
                recipe.UsedIngredients.Count(i => VegetableIngredients.Contains(i.Name)) * 10.0,

            RecipeGoal.TastyFirst =>
                coverageScore * 0.4 + popularityBonus * 0.6,

            _ => coverageScore
        };
    }

    private static string GenerateGoalReason(SpoonacularRecipeSummary recipe, RecipeGoal goal)
    {
        var n = recipe.UsedIngredientCount;
        return goal switch
        {
            RecipeGoal.ProteinFirst =>
                $"Uses {n} of your ingredients with a focus on protein sources.",
            RecipeGoal.LowCalories =>
                $"Light recipe using {n} of your available ingredients.",
            RecipeGoal.TastyFirst =>
                $"Popular recipe using {n} of your ingredients.",
            _ => $"Uses {n} of your available ingredients."
        };
    }

    private static RecipeGoal ParseGoal(string goal) => goal.ToLowerInvariant() switch
    {
        "protein_first" => RecipeGoal.ProteinFirst,
        "low_calories" => RecipeGoal.LowCalories,
        "tasty_first" => RecipeGoal.TastyFirst,
        _ => throw new AppValidationException(
            $"Invalid goal '{goal}'. Accepted values: protein_first, low_calories, tasty_first.")
    };
}
