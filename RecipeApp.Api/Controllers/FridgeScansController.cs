using Microsoft.AspNetCore.Mvc;
using RecipeApp.Api.Contracts.Requests;
using RecipeApp.Api.Services;

namespace RecipeApp.Api.Controllers;

[ApiController]
[Route("api/fridge-scans")]
public class FridgeScansController(FridgeScanService fridgeScanService) : ControllerBase
{
    /// <summary>Upload 1-2 ingredient photos and detect ingredients with AI.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Create(
        [FromForm] string deviceId,
        IFormFileCollection images)
    {
        var result = await fridgeScanService.CreateScanAsync(deviceId, images);
        return Ok(result);
    }

    /// <summary>Confirm or edit the detected ingredient list before searching recipes.</summary>
    [HttpPost("{scanId:guid}/confirm-ingredients")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmIngredients(
        Guid scanId,
        [FromBody] ConfirmIngredientsRequest req)
    {
        var result = await fridgeScanService.ConfirmIngredientsAsync(scanId, req);
        return Ok(result);
    }
}
