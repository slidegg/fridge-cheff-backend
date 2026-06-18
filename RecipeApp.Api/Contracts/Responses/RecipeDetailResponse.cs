namespace RecipeApp.Api.Contracts.Responses;

public record RecipeDetailResponse(
    int Id,
    string Title,
    string ImageUrl,
    int Servings,
    int ReadyInMinutes,
    string? SourceUrl,
    MacrosDto? MacrosPerServing,
    List<RecipeIngredientDto> Ingredients,
    List<RecipeStepDto> Steps,
    List<string> MissingIngredients,
    string MacroDisclaimer
);

public record MacrosDto(
    double Calories,
    double ProteinG,
    double CarbsG,
    double FatG
);

public record RecipeIngredientDto(
    string Name,
    double Amount,
    string Unit
);

public record RecipeStepDto(
    int Number,
    string Text
);
