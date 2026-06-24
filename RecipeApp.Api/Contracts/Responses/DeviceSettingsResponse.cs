namespace RecipeApp.Api.Contracts.Responses;

public record DeviceSettingsResponse(
    bool AllowMissingIngredients,
    int MaxMissingIngredients,
    bool IgnorePantry,
    List<string> AlwaysAvailableIngredients
);
