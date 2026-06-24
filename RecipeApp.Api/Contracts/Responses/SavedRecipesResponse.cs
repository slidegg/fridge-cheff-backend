namespace RecipeApp.Api.Contracts.Responses;

public record SavedRecipesResponse(
    List<SavedRecipeDto> Recipes
);

public record SavedRecipeDto(
    int Id,
    string Title,
    string ImageUrl,
    string SavedAtUtc
);
