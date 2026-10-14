using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using UnitConversion.Application.DTOs;
using UnitConversion.Application.Interfaces;

namespace UnitConversion.Api.Controllers;

/// <summary>
/// Endpoints for inspecting supported measurement categories and units.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public sealed class CategoriesController : ControllerBase
{
    private readonly IConversionService _conversionService;

    public CategoriesController(IConversionService conversionService)
    {
        _conversionService = conversionService ?? throw new ArgumentNullException(nameof(conversionService));
    }

    /// <summary>
    /// Retrieves all supported measurement categories.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of available categories with base unit and unit count.</returns>
    /// <response code="200">List of categories successfully retrieved.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        var categories = await _conversionService.GetCategoriesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(categories);
    }

    /// <summary>
    /// Retrieves all supported measurement units and their symbols for a given category.
    /// </summary>
    /// <param name="category">The category identifier or name (e.g. "length", "temperature", "weight"). Case-insensitive.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of units with ID, name, and symbol.</returns>
    /// <response code="200">List of units successfully retrieved.</response>
    /// <response code="404">Specified category was not found.</response>
    [HttpGet("{category}/units")]
    [ProducesResponseType(typeof(IReadOnlyList<UnitDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<UnitDto>>> GetUnitsByCategoryAsync(
        [FromRoute] string category,
        CancellationToken cancellationToken)
    {
        var units = await _conversionService.GetUnitsByCategoryAsync(category, cancellationToken).ConfigureAwait(false);
        return Ok(units);
    }
}
