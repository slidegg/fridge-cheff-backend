namespace RecipeApp.Api.Data.Entities;

public class ScanImage
{
    public Guid Id { get; set; }
    public Guid FridgeScanId { get; set; }
    public string ImagePath { get; set; } = default!;
    public string OriginalFileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long SizeBytes { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public FridgeScan FridgeScan { get; set; } = default!;
}
