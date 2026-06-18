namespace RecipeApp.Api.Options;

public class SpoonacularOptions
{
    public const string Section = "Spoonacular";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.spoonacular.com";
}
