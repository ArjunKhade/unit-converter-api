using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using UnitConversion.Application.DTOs;
using UnitConversion.Application.Interfaces;

namespace UnitConversion.Api.Controllers;

/// <summary>
/// Endpoints for executing unit conversions between units of measurement.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public sealed class ConversionsController : ControllerBase
{
    private readonly IConversionService _conversionService;

    public ConversionsController(IConversionService conversionService)
    {
        _conversionService = conversionService ?? throw new ArgumentNullException(nameof(conversionService));
    }

    /// <summary>
    /// Converts a numerical value from one unit to another within a specified measurement category.
    /// </summary>
    /// <param name="request">The conversion parameters specifying category, fromUnit, toUnit, and numerical value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Conversion succeeded and result returned.</response>
    /// <response code="400">Invalid parameters (e.g. unknown unit, unit not in category, temperature below absolute zero).</response>
    /// <response code="404">Category does not exist.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ConversionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversionResponseDto>> ConvertAsync(
        [FromBody] ConversionRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _conversionService.ConvertAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }
}
