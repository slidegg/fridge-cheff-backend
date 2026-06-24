using System.ComponentModel.DataAnnotations;

namespace RecipeApp.Api.Contracts.Requests;

public record UpdateDeviceSettingsRequest
{
    [Required]
    public string DeviceId { get; init; } = default!;

    public bool AllowMissingIngredients { get; init; }

    [Range(0, 10)]
    public int MaxMissingIngredients { get; init; }

    public bool IgnorePantry { get; init; } = true;

    [Required]
    public List<string> AlwaysAvailableIngredients { get; init; } = [];
}
