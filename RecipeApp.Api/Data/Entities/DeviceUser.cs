namespace RecipeApp.Api.Data.Entities;

public class DeviceUser
{
    public Guid Id { get; set; }
    public string DeviceId { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastSeenAtUtc { get; set; }

    public ICollection<FridgeScan> Scans { get; set; } = [];
    public ICollection<RecipeSearch> RecipeSearches { get; set; } = [];
    public ICollection<RecipeInteraction> Interactions { get; set; } = [];
    public ICollection<SavedRecipe> SavedRecipes { get; set; } = [];
    public ICollection<UsageCounter> UsageCounters { get; set; } = [];
}
