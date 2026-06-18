namespace RecipeApp.Api.Options;

public class OpenAiOptions
{
    public const string Section = "OpenAi";

    public string ApiKey { get; set; } = string.Empty;
    public string VisionModel { get; set; } = "gpt-4o";
    public int MaxTokens { get; set; } = 1000;
}
