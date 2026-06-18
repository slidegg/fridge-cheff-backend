using System.ComponentModel.DataAnnotations;

namespace RecipeApp.Api.Contracts.Requests;

public record SaveRecipeRequest
{
    [Required]
    public string DeviceId { get; init; } = default!;

    [Required]
    public string Title { get; init; } = default!;

    [Required]
    public string ImageUrl { get; init; } = default!;
}
