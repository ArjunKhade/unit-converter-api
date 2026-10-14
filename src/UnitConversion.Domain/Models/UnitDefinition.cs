namespace UnitConversion.Domain.Models;

/// <summary>
/// Represents a unit of measurement within a category.
/// Uses an affine transformation formula relative to the base unit:
/// BaseValue = (Value * Factor) + Offset
/// Value = (BaseValue - Offset) / Factor
/// For linear units (e.g. length, mass), Offset is 0.
/// </summary>
public sealed record UnitDefinition
{
    /// <summary>
    /// Gets the unique canonical identifier for the unit (e.g. "meter", "celsius", "pound").
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the human-readable display name for the unit (e.g. "Meter", "Celsius", "Pound").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the standard symbol for the unit (e.g. "m", "°C", "lb").
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Gets the identifier of the category this unit belongs to.
    /// </summary>
    public required string CategoryId { get; init; }

    /// <summary>
    /// Gets the multiplicative factor relative to the base unit.
    /// </summary>
    public required decimal Factor { get; init; }

    /// <summary>
    /// Gets the additive offset relative to the base unit. Default is 0.
    /// </summary>
    public decimal Offset { get; init; } = 0m;

    /// <summary>
    /// Gets aliases for matching user inputs (case-insensitive names, plurals, symbols).
    /// </summary>
    public IReadOnlyList<string> Aliases { get; init; } = [];

    /// <summary>
    /// Converts a value from this unit to the base unit of its category.
    /// </summary>
    /// <param name="value">The value in this unit.</param>
    /// <returns>The equivalent value in the base unit.</returns>
    public decimal ToBaseValue(decimal value) => (value * Factor) + Offset;

    /// <summary>
    /// Converts a value from the base unit of its category to this unit.
    /// </summary>
    /// <param name="baseValue">The value in the base unit.</param>
    /// <returns>The equivalent value in this unit.</returns>
    public decimal FromBaseValue(decimal baseValue) => (baseValue - Offset) / Factor;
}
