namespace UnitConversion.Application.DTOs;

/// <summary>
/// Response payload containing the result of a unit conversion.
/// </summary>
public sealed record ConversionResponseDto
{
    /// <summary>
    /// The measurement category.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// The resolved source unit.
    /// </summary>
    public required string FromUnit { get; init; }

    /// <summary>
    /// The resolved target unit.
    /// </summary>
    public required string ToUnit { get; init; }

    /// <summary>
    /// The original numerical value supplied in the request.
    /// </summary>
    public required decimal InputValue { get; init; }

    /// <summary>
    /// The converted numerical result, rounded according to precision settings.
    /// </summary>
    public required decimal Result { get; init; }
}
