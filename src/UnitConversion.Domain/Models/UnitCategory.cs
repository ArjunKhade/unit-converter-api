namespace UnitConversion.Domain.Models;

/// <summary>
/// Represents a category of measurement units (e.g. Length, Temperature, Weight).
/// </summary>
public sealed record UnitCategory
{
    /// <summary>
    /// Gets the canonical identifier for the category (e.g., "length", "temperature", "weight").
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the human-readable display name for the category (e.g., "Length", "Temperature", "Weight / Mass").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the canonical identifier of the base unit for this category.
    /// </summary>
    public required string BaseUnitId { get; init; }

    /// <summary>
    /// Gets an optional description of the category.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets alternative names or aliases for the category (e.g. "mass" for "weight").
    /// </summary>
    public IReadOnlyList<string> Aliases { get; init; } = [];

    /// <summary>
    /// Gets the minimum allowable value in the category's base unit, if applicable (e.g., 0 K for temperature).
    /// </summary>
    public decimal? MinAllowedBaseValue { get; init; }
}
