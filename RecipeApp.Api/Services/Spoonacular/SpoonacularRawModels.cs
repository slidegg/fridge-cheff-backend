// Internal JSON deserialization shapes for Spoonacular API responses.
// These are never exposed outside this assembly.
namespace RecipeApp.Api.Services.Spoonacular;

internal sealed record SpoonacularFindByIngredientsItem(
    int Id, string? Title, string? Image,
    int UsedIngredientCount, int MissedIngredientCount,
    List<SpoonacularRawIngredient>? UsedIngredients,
    List<SpoonacularRawIngredient>? MissedIngredients,
    int Likes);

internal sealed record SpoonacularRawIngredient(int Id, string? Name, double Amount, string? Unit);

internal sealed record SpoonacularRecipeInfoResponse(
    int Id, string? Title, string? Image, int Servings, int ReadyInMinutes, string? SourceUrl,
    List<SpoonacularRawExtendedIngredient>? ExtendedIngredients,
    List<SpoonacularRawInstruction>? AnalyzedInstructions,
    SpoonacularRawNutrition? Nutrition);

internal sealed record SpoonacularRawExtendedIngredient(string? Name, double Amount, string? Unit);
internal sealed record SpoonacularRawInstruction(List<SpoonacularRawStep>? Steps);
internal sealed record SpoonacularRawStep(int Number, string? Step);
internal sealed record SpoonacularRawNutrition(List<SpoonacularRawNutrient>? Nutrients);
internal sealed record SpoonacularRawNutrient(string? Name, double Amount, string? Unit);
