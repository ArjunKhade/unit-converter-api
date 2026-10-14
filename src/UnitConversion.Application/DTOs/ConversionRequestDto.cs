using System.ComponentModel.DataAnnotations;

namespace UnitConversion.Application.DTOs;

/// <summary>
/// Request payload for converting a value between two units in a category.
/// </summary>
public sealed record ConversionRequestDto
{
    /// <summary>
    /// The measurement category (e.g. "length", "temperature", "weight"). Case-insensitive.
    /// </summary>
    [Required(ErrorMessage = "Category is required.")]
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// The source unit name, alias, or symbol (e.g. "meter", "m", "Celsius"). Case-insensitive.
    /// </summary>
    [Required(ErrorMessage = "fromUnit is required.")]
    public string FromUnit { get; init; } = string.Empty;

    /// <summary>
    /// The destination unit name, alias, or symbol (e.g. "foot", "ft", "Fahrenheit"). Case-insensitive.
    /// </summary>
    [Required(ErrorMessage = "toUnit is required.")]
    public string ToUnit { get; init; } = string.Empty;

    /// <summary>
    /// The numerical value to convert.
    /// </summary>
    [Required(ErrorMessage = "Value is required.")]
    public decimal Value { get; init; }

    /// <summary>
    /// Optional custom decimal precision for rounding the result (overrides default).
    /// </summary>
    [Range(0, 15, ErrorMessage = "Precision must be between 0 and 15.")]
    public int? Precision { get; init; }
}
