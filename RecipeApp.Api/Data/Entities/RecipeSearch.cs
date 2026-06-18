namespace RecipeApp.Api.Data.Entities;

public class RecipeSearch
{
    public Guid Id { get; set; }
    public Guid DeviceUserId { get; set; }
    public Guid? FridgeScanId { get; set; }
    public RecipeGoal Goal { get; set; }
    public string IngredientsJson { get; set; } = default!;
    public int ResultCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public DeviceUser DeviceUser { get; set; } = default!;
}
