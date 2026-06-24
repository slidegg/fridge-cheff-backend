using Microsoft.AspNetCore.Mvc;
using RecipeApp.Api.Contracts.Requests;
using RecipeApp.Api.Services;

namespace RecipeApp.Api.Controllers;

[ApiController]
[Route("api/recipes")]
public class RecipesController(RecipeService recipeService) : ControllerBase
{
    /// <summary>Suggest recipes based on available ingredients and goal.</summary>
    [HttpPost("suggest")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Suggest([FromBody] SuggestRecipesRequest req)
    {
        var result = await recipeService.SuggestAsync(req);
        return Ok(result);
    }

    /// <summary>Get the device's saved recipes.</summary>
    [HttpGet("saved")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSaved([FromQuery] string deviceId)
    {
        var result = await recipeService.GetSavedRecipesAsync(deviceId);
        return Ok(result);
    }

    /// <summary>Get full recipe detail with macros, steps, and missing ingredients.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetDetail(int id, [FromQuery] string deviceId)
    {
        var result = await recipeService.GetDetailAsync(id, deviceId);
        return Ok(result);
    }

    /// <summary>Save a recipe to the user's saved list.</summary>
    [HttpPost("{id:int}/save")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Save(int id, [FromBody] SaveRecipeRequest req)
    {
        await recipeService.SaveRecipeAsync(id, req);
        return Ok(new { saved = true });
    }
}
