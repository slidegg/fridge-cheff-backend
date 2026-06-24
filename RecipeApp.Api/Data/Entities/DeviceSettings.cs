namespace RecipeApp.Api.Data.Entities;

public class DeviceSettings
{
    public Guid Id { get; set; }
    public Guid DeviceUserId { get; set; }
    public bool AllowMissingIngredients { get; set; }
    public int MaxMissingIngredients { get; set; }

    /// <summary>Passed directly as Spoonacular's findByIngredients ignorePantry parameter.</summary>
    public bool IgnorePantry { get; set; } = true;

    /// <summary>JSON string array — ingredients the user always has (pasta, rice, etc.), merged into every search.</summary>
    public string AlwaysAvailableIngredientsJson { get; set; } = default!;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public DeviceUser DeviceUser { get; set; } = default!;
}
