using System.ComponentModel.DataAnnotations;

namespace RecipeApp.Api.Contracts.Requests;

public record ConfirmIngredientsRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one ingredient is required.")]
    public List<string> Ingredients { get; init; } = [];
}
