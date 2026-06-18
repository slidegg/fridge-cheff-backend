namespace RecipeApp.Api.Contracts.Responses;

public record SuggestRecipesResponse(
    List<RecipeSummaryDto> Recipes,
    EmptyStateDto? EmptyState
);

public record RecipeSummaryDto(
    int Id,
    string Title,
    string ImageUrl,
    List<string> UsedIngredients,
    List<string> MissedIngredients,
    int UsedIngredientCount,
    int MissedIngredientCount,
    int ReadyInMinutes,
    double Score,
    string GoalReason
);

public record EmptyStateDto(
    string Message,
    List<string> Suggestions
);
