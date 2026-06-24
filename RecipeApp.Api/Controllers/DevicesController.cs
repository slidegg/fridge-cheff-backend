using Microsoft.AspNetCore.Mvc;
using RecipeApp.Api.Contracts.Requests;
using RecipeApp.Api.Services;

namespace RecipeApp.Api.Controllers;

[ApiController]
[Route("api/devices")]
public class DevicesController(DeviceService deviceService, DeviceSettingsService settingsService) : ControllerBase
{
    /// <summary>Register a device or return existing registration.</summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterDeviceRequest req)
    {
        var result = await deviceService.RegisterAsync(req);
        return Ok(result);
    }

    /// <summary>Get the device's recipe search and pantry preferences.</summary>
    [HttpGet("settings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSettings([FromQuery] string deviceId)
    {
        var deviceUser = await deviceService.GetByDeviceIdAsync(deviceId);
        var settings = await settingsService.GetSettingsAsync(deviceUser.Id);
        return Ok(settings);
    }

    /// <summary>Update the device's recipe search and pantry preferences.</summary>
    [HttpPut("settings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateDeviceSettingsRequest req)
    {
        var deviceUser = await deviceService.GetByDeviceIdAsync(req.DeviceId);
        var settings = await settingsService.UpdateSettingsAsync(deviceUser.Id, req);
        return Ok(settings);
    }
}
