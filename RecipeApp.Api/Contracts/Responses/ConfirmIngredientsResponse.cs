namespace RecipeApp.Api.Contracts.Responses;

public record ConfirmIngredientsResponse(
    Guid ScanId,
    List<string> ConfirmedIngredients
);
