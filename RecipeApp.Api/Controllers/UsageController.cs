using Microsoft.AspNetCore.Mvc;
using RecipeApp.Api.Services;

namespace RecipeApp.Api.Controllers;

[ApiController]
[Route("api/usage")]
public class UsageController(DeviceService deviceService, UsageLimitService usageLimitService) : ControllerBase
{
    /// <summary>Get today's usage counters for a device.</summary>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyUsage([FromQuery] string deviceId)
    {
        var deviceUser = await deviceService.GetByDeviceIdAsync(deviceId);
        var result = await usageLimitService.GetUsageAsync(deviceUser.Id);
        return Ok(result);
    }
}
