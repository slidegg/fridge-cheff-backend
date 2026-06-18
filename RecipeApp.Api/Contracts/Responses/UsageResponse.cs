namespace RecipeApp.Api.Contracts.Responses;

public record UsageResponse(
    string DateUtc,
    int ScansUsed,
    int ScanLimit,
    int RecipeSearchesUsed,
    int RecipeSearchLimit,
    int RecipeDetailsUsed,
    int RecipeDetailLimit
);
