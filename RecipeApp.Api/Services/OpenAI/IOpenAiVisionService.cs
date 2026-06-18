namespace RecipeApp.Api.Services.OpenAI;

public interface IOpenAiVisionService
{
    Task<OpenAiVisionResult> DetectIngredientsAsync(IFormFileCollection images);
}
