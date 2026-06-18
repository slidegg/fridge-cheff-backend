namespace RecipeApp.Api.Contracts.Responses;

public record FridgeScanResponse(
    Guid ScanId,
    List<DetectedIngredientDto> DetectedItems,
    List<string> Warnings
);

public record DetectedIngredientDto(
    string Name,
    string NormalizedName,
    string Category,
    float Confidence,
    string? QuantityVisible,
    bool NeedsConfirmation
);
