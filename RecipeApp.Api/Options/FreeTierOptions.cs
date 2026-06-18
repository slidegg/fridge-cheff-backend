namespace RecipeApp.Api.Options;

public class FreeTierOptions
{
    public const string Section = "FreeTier";

    public int DailyScanLimit { get; set; } = 1;
    public int DailyRecipeSearchLimit { get; set; } = 3;
    public int DailyRecipeDetailLimit { get; set; } = 3;
    public int MaxSuggestionsPerSearch { get; set; } = 8;
    public int MaxImagesPerScan { get; set; } = 2;
}
