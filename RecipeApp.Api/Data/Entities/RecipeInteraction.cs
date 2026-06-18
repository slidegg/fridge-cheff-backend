namespace RecipeApp.Api.Data.Entities;

public class RecipeInteraction
{
    public Guid Id { get; set; }
    public Guid DeviceUserId { get; set; }
    public int SpoonacularRecipeId { get; set; }
    public InteractionAction Action { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public DeviceUser DeviceUser { get; set; } = default!;
}
