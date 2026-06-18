namespace RecipeApp.Api.Services.OpenAI;

public class MockOpenAiVisionService(ILogger<MockOpenAiVisionService> logger) : IOpenAiVisionService
{
    public Task<OpenAiVisionResult> DetectIngredientsAsync(IFormFileCollection images)
    {
        logger.LogInformation("Mock: detecting ingredients from {Count} image(s)", images.Count);

        var result = new OpenAiVisionResult(
            Items:
            [
                new("chicken breast", "chicken breast", "protein", 0.95f, "2 pieces", true, 0),
                new("eggs", "eggs", "protein", 0.97f, "6 eggs", true, 0),
                new("spinach", "spinach", "vegetable", 0.91f, "1 bag", true, 0),
                new("greek yogurt", "greek yogurt", "dairy", 0.88f, "1 container", true, 0),
                new("cheddar cheese", "cheddar cheese", "dairy", 0.85f, "1 block", true, 0),
                new("broccoli", "broccoli", "vegetable", 0.93f, "1 head", true, 0),
                new("unknown sauce bottle", "unknown sauce", "condiment", 0.45f, "1 bottle", true, 0),
            ],
            Warnings:
            [
                "Quantities are visual estimates and should be confirmed.",
                "Some items could not be identified with high confidence.",
            ]
        );

        return Task.FromResult(result);
    }
}
