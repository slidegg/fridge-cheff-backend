namespace RecipeApp.Api.Data.Entities;

public class FridgeScan
{
    public Guid Id { get; set; }
    public Guid DeviceUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int ImageCount { get; set; }
    public ScanStatus Status { get; set; }
    public string? ErrorMessage { get; set; }

    public DeviceUser DeviceUser { get; set; } = default!;
    public ICollection<ScanImage> Images { get; set; } = [];
    public ICollection<DetectedIngredient> DetectedIngredients { get; set; } = [];
    public ICollection<ConfirmedIngredient> ConfirmedIngredients { get; set; } = [];
}
