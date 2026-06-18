namespace RecipeApp.Api.Data.Entities;

public class SavedRecipe
{
    public Guid Id { get; set; }
    public Guid DeviceUserId { get; set; }
    public int SpoonacularRecipeId { get; set; }
    public string Title { get; set; } = default!;
    public string ImageUrl { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }

    public DeviceUser DeviceUser { get; set; } = default!;
}
