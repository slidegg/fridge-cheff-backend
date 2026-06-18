namespace RecipeApp.Api.Services.OpenAI;

public record OpenAiVisionResult(
    List<OpenAiDetectedItem> Items,
    List<string> Warnings
);

public record OpenAiDetectedItem(
    string Name,
    string NormalizedName,
    string Category,
    float Confidence,
    string? QuantityVisible,
    bool NeedsConfirmation,
    int? SourceImageIndex
);
