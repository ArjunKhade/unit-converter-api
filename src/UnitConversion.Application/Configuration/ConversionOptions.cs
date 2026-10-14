namespace UnitConversion.Application.Configuration;

/// <summary>
/// Strongly typed options for unit conversion operations.
/// </summary>
public sealed class ConversionOptions
{
    public const string SectionName = "ConversionOptions";

    /// <summary>
    /// Default number of decimal places to round conversion results to.
    /// Default is 4, producing 32.8084 for 10 meters to feet.
    /// </summary>
    public int DefaultPrecision { get; set; } = 4;

    /// <summary>
    /// Maximum allowed decimal places for rounding. Default is 15.
    /// </summary>
    public int MaxPrecision { get; set; } = 15;
}
