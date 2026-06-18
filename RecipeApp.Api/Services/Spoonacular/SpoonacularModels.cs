namespace RecipeApp.Api.Services.Spoonacular;

public record SpoonacularRecipeSummary(
    int Id,
    string Title,
    string Image,
    int UsedIngredientCount,
    int MissedIngredientCount,
    List<SpoonacularIngredient> UsedIngredients,
    List<SpoonacularIngredient> MissedIngredients,
    int Likes
);

public record SpoonacularIngredient(
    int Id,
    string Name,
    double Amount,
    string Unit
);

public record SpoonacularRecipeDetail(
    int Id,
    string Title,
    string Image,
    int Servings,
    int ReadyInMinutes,
    string? SourceUrl,
    List<SpoonacularExtendedIngredient> ExtendedIngredients,
    List<SpoonacularStep> Steps,
    SpoonacularNutrition? Nutrition
);

public record SpoonacularExtendedIngredient(
    string Name,
    double Amount,
    string Unit
);

public record SpoonacularStep(
    int Number,
    string Step
);

public record SpoonacularNutrition(
    List<SpoonacularNutrient> Nutrients
);

public record SpoonacularNutrient(
    string Name,
    double Amount,
    string Unit
);
