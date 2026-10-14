namespace UnitConversion.Application.DTOs;

/// <summary>
/// Data transfer object representing a unit of measurement.
/// </summary>
public sealed record UnitDto
{
    /// <summary>
    /// The unique canonical identifier for the unit (e.g. "meter", "celsius").
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The display name of the unit (e.g. "Meter", "Celsius").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The standard symbol for the unit (e.g. "m", "°C").
    /// </summary>
    public required string Symbol { get; init; }
}
