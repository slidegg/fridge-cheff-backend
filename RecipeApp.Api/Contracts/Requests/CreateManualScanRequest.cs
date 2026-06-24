using System.ComponentModel.DataAnnotations;

namespace RecipeApp.Api.Contracts.Requests;

public record CreateManualScanRequest
{
    [Required]
    public string DeviceId { get; init; } = default!;
}
