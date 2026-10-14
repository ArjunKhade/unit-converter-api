namespace UnitConversion.Application.DTOs;

/// <summary>
/// Data transfer object representing a unit category.
/// </summary>
public sealed record CategoryDto
{
    /// <summary>
    /// The unique category identifier (e.g. "length").
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The display name of the category (e.g. "Length").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The base unit identifier for the category (e.g. "meter").
    /// </summary>
    public required string BaseUnit { get; init; }

    /// <summary>
    /// The number of registered units in this category.
    /// </summary>
    public required int UnitCount { get; init; }

    /// <summary>
    /// Optional description of the category.
    /// </summary>
    public string? Description { get; init; }
}
