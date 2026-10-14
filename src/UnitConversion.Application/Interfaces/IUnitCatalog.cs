using UnitConversion.Domain.Models;

namespace UnitConversion.Application.Interfaces;

/// <summary>
/// Abstraction for accessing unit categories and definitions.
/// Allows swapping the in-memory catalog for a database or configuration file without altering business logic.
/// </summary>
public interface IUnitCatalog
{
    /// <summary>
    /// Retrieves all available unit categories.
    /// </summary>
    Task<IReadOnlyList<UnitCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a category by its ID or alias (case-insensitive).
    /// </summary>
    Task<UnitCategory?> GetCategoryAsync(string categoryIdOrAlias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all units belonging to a specific category.
    /// </summary>
    Task<IReadOnlyList<UnitDefinition>> GetUnitsByCategoryAsync(string categoryIdOrAlias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a unit by its ID, name, symbol, or alias within a category (case-insensitive).
    /// </summary>
    Task<UnitDefinition?> FindUnitAsync(string categoryIdOrAlias, string unitNameOrSymbol, CancellationToken cancellationToken = default);
}
