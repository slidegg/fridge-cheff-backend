using System.ComponentModel.DataAnnotations;

namespace RecipeApp.Api.Contracts.Requests;

public record RegisterDeviceRequest
{
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string DeviceId { get; init; } = default!;
}
