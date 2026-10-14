using UnitConversion.Application.DTOs;

namespace UnitConversion.Application.Interfaces;

/// <summary>
/// Service contract for executing unit conversions and category queries.
/// </summary>
public interface IConversionService
{
    /// <summary>
    /// Converts a numerical value from one unit to another within a category.
    /// </summary>
    /// <param name="request">The conversion request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The conversion response containing the calculated result.</returns>
    Task<ConversionResponseDto> ConvertAsync(ConversionRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all available categories.
    /// </summary>
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all units belonging to a specific category.
    /// </summary>
    Task<IReadOnlyList<UnitDto>> GetUnitsByCategoryAsync(string categoryId, CancellationToken cancellationToken = default);
}
