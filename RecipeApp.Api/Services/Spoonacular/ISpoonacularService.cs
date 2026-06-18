namespace RecipeApp.Api.Services.Spoonacular;

public interface ISpoonacularService
{
    Task<List<SpoonacularRecipeSummary>> FindByIngredientsAsync(
        IEnumerable<string> ingredients,
        int number = 20,
        int ranking = 2,
        bool ignorePantry = true);

    Task<SpoonacularRecipeDetail?> GetRecipeDetailAsync(int recipeId);
}
