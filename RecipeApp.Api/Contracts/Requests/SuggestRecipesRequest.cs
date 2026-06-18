using System.ComponentModel.DataAnnotations;

namespace RecipeApp.Api.Contracts.Requests;

public record SuggestRecipesRequest
{
    [Required]
    public string DeviceId { get; init; } = default!;

    public Guid? ScanId { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one ingredient is required.")]
    public List<string> Ingredients { get; init; } = [];

    /// <summary>protein_first | low_calories | tasty_first</summary>
    [Required]
    public string Goal { get; init; } = default!;
}
