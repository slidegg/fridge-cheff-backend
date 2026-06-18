namespace RecipeApp.Api.Services;

public class IngredientNormalizationService
{
    public string Normalize(string name) =>
        name.Trim().ToLowerInvariant();

    public List<string> NormalizeList(IEnumerable<string> names) =>
        names.Select(Normalize).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();
}
