namespace RecipeApp.Api.Data.Entities;

public class ConfirmedIngredient
{
    public Guid Id { get; set; }
    public Guid FridgeScanId { get; set; }
    public string Name { get; set; } = default!;
    public string NormalizedName { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }

    public FridgeScan FridgeScan { get; set; } = default!;
}
