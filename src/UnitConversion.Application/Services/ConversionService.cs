using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UnitConversion.Application.Configuration;
using UnitConversion.Application.DTOs;
using UnitConversion.Application.Interfaces;
using UnitConversion.Domain.Exceptions;
using UnitConversion.Domain.Models;

namespace UnitConversion.Application.Services;

/// <summary>
/// Core implementation of unit conversion logic using affine base-unit modeling.
/// </summary>
public sealed partial class ConversionService : IConversionService
{
    private readonly IUnitCatalog _unitCatalog;
    private readonly ConversionOptions _options;
    private readonly ILogger<ConversionService> _logger;

    public ConversionService(
        IUnitCatalog unitCatalog,
        IOptions<ConversionOptions> options,
        ILogger<ConversionService> logger)
    {
        _unitCatalog = unitCatalog ?? throw new ArgumentNullException(nameof(unitCatalog));
        _options = options?.Value ?? new ConversionOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ConversionResponseDto> ConvertAsync(ConversionRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Category))
        {
            throw new UnitConversionValidationException(nameof(request.Category), "Category cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(request.FromUnit))
        {
            throw new UnitConversionValidationException(nameof(request.FromUnit), "fromUnit cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(request.ToUnit))
        {
            throw new UnitConversionValidationException(nameof(request.ToUnit), "toUnit cannot be empty.");
        }

        LogConversionExecuting(_logger, request.Category, request.FromUnit, request.ToUnit, request.Value);

        // 1. Resolve Category
        var category = await _unitCatalog.GetCategoryAsync(request.Category, cancellationToken).ConfigureAwait(false);
        if (category is null)
        {
            LogCategoryNotFound(_logger, request.Category);
            throw new CategoryNotFoundException(request.Category);
        }

        // 2. Resolve FromUnit
        var fromUnit = await _unitCatalog.FindUnitAsync(category.Id, request.FromUnit, cancellationToken).ConfigureAwait(false);
        if (fromUnit is null)
        {
            LogSourceUnitNotFound(_logger, request.FromUnit, category.Id);
            throw new UnitNotFoundException(request.FromUnit, category.Id);
        }

        // 3. Resolve ToUnit
        var toUnit = await _unitCatalog.FindUnitAsync(category.Id, request.ToUnit, cancellationToken).ConfigureAwait(false);
        if (toUnit is null)
        {
            LogTargetUnitNotFound(_logger, request.ToUnit, category.Id);
            throw new UnitNotFoundException(request.ToUnit, category.Id);
        }

        // 4. Convert input to base unit
        decimal baseValue = fromUnit.ToBaseValue(request.Value);

        // 5. Enforce physical constraints (e.g., Absolute Zero for temperature)
        if (category.MinAllowedBaseValue.HasValue && baseValue < category.MinAllowedBaseValue.Value)
        {
            LogBelowAbsoluteZero(_logger, request.Value, fromUnit.Symbol, baseValue, category.MinAllowedBaseValue.Value, category.Id);

            throw new UnitConversionValidationException(
                nameof(request.Value),
                $"The supplied value '{request.Value}' {fromUnit.Symbol} evaluates to {baseValue} in base units, which is below absolute zero (0 K / -273.15 °C / -459.67 °F).");
        }

        // 6. Convert from base unit to target unit
        decimal rawResult;
        if (string.Equals(fromUnit.Id, toUnit.Id, StringComparison.OrdinalIgnoreCase))
        {
            rawResult = request.Value;
        }
        else
        {
            rawResult = toUnit.FromBaseValue(baseValue);
        }

        // 7. Apply rounding
        int precision = request.Precision ?? _options.DefaultPrecision;
        precision = Math.Clamp(precision, 0, _options.MaxPrecision);
        decimal roundedResult = Math.Round(rawResult, precision, MidpointRounding.AwayFromZero);

        LogConversionSuccess(_logger, request.Value, fromUnit.Id, roundedResult, toUnit.Id, category.Id, precision);

        return new ConversionResponseDto
        {
            Category = category.Id,
            FromUnit = fromUnit.Id,
            ToUnit = toUnit.Id,
            InputValue = request.Value,
            Result = roundedResult
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _unitCatalog.GetCategoriesAsync(cancellationToken).ConfigureAwait(false);
        var result = new List<CategoryDto>(categories.Count);

        foreach (var category in categories)
        {
            var units = await _unitCatalog.GetUnitsByCategoryAsync(category.Id, cancellationToken).ConfigureAwait(false);
            result.Add(new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                BaseUnit = category.BaseUnitId,
                UnitCount = units.Count,
                Description = category.Description
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UnitDto>> GetUnitsByCategoryAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(categoryId))
        {
            throw new UnitConversionValidationException(nameof(categoryId), "Category cannot be empty.");
        }

        var category = await _unitCatalog.GetCategoryAsync(categoryId, cancellationToken).ConfigureAwait(false);
        if (category is null)
        {
            throw new CategoryNotFoundException(categoryId);
        }

        var units = await _unitCatalog.GetUnitsByCategoryAsync(category.Id, cancellationToken).ConfigureAwait(false);
        return units.Select(u => new UnitDto
        {
            Id = u.Id,
            Name = u.Name,
            Symbol = u.Symbol
        }).ToList();
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Executing conversion request for category '{Category}' from '{FromUnit}' to '{ToUnit}' with value {Value}")]
    private static partial void LogConversionExecuting(ILogger logger, string category, string fromUnit, string toUnit, decimal value);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Conversion failed: Category '{Category}' was not found")]
    private static partial void LogCategoryNotFound(ILogger logger, string category);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Conversion failed: Source unit '{FromUnit}' not found in category '{Category}'")]
    private static partial void LogSourceUnitNotFound(ILogger logger, string fromUnit, string category);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Conversion failed: Target unit '{ToUnit}' not found in category '{Category}'")]
    private static partial void LogTargetUnitNotFound(ILogger logger, string toUnit, string category);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Conversion failed: Value {Value} {FromUnit} produces base value {BaseValue} which is below minimum allowed {MinBaseValue} in category '{Category}'")]
    private static partial void LogBelowAbsoluteZero(ILogger logger, decimal value, string fromUnit, decimal baseValue, decimal minBaseValue, string category);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Successfully converted {Value} {FromUnit} to {Result} {ToUnit} (Category: {Category}, Precision: {Precision})")]
    private static partial void LogConversionSuccess(ILogger logger, decimal value, string fromUnit, decimal result, string toUnit, string category, int precision);
}
