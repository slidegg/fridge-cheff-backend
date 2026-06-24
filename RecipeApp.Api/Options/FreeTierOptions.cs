namespace RecipeApp.Api.Options;

public class FreeTierOptions
{
    public const string Section = "FreeTier";

    public int DailyScanLimit { get; set; } = 1;
    public int DailyRecipeSearchLimit { get; set; } = 3;
    public int DailyRecipeDetailLimit { get; set; } = 3;
    public int MaxSuggestionsPerSearch { get; set; } = 8;
    public int MaxImagesPerScan { get; set; } = 4;

    /// <summary>When true, daily limits are never enforced. Usage is still tracked. Dev/QA only — never enable in production.</summary>
    public bool UnlimitedUsage { get; set; } = false;
}
