using Microsoft.AspNetCore.Mvc;
using RecipeApp.Api.Contracts.Requests;
using RecipeApp.Api.Services;

namespace RecipeApp.Api.Controllers;

[ApiController]
[Route("api/devices")]
public class DevicesController(DeviceService deviceService) : ControllerBase
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
}
