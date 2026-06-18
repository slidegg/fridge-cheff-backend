namespace RecipeApp.Api.Contracts.Responses;

public record RegisterDeviceResponse(
    Guid DeviceUserId,
    string DeviceId
);
