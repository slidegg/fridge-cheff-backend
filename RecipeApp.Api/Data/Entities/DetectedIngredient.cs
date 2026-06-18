namespace RecipeApp.Api.Data.Entities;

public class DetectedIngredient
{
    public Guid Id { get; set; }
    public Guid FridgeScanId { get; set; }
    public string Name { get; set; } = default!;
    public string NormalizedName { get; set; } = default!;
    public string Category { get; set; } = default!;
    public float Confidence { get; set; }
    public string? QuantityVisible { get; set; }
    public bool NeedsConfirmation { get; set; }
    public int? SourceImageIndex { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public FridgeScan FridgeScan { get; set; } = default!;
}
