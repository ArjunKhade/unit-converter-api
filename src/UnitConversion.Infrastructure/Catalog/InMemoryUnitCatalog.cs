using UnitConversion.Application.Interfaces;
using UnitConversion.Domain.Models;

namespace UnitConversion.Infrastructure.Catalog;

/// <summary>
/// In-memory catalog containing predefined measurement categories and units.
/// Implements affine modeling (Factor + Offset) and fast O(1) case-insensitive indexing.
/// Can be replaced by an EF Core or JSON repository without touching the application layer.
/// </summary>
public sealed class InMemoryUnitCatalog : IUnitCatalog
{
    private readonly List<UnitCategory> _categories;
    private readonly Dictionary<string, UnitCategory> _categoryLookup;
    private readonly Dictionary<string, List<UnitDefinition>> _unitsByCategory;
    private readonly Dictionary<(string CategoryId, string LookupKey), UnitDefinition> _unitLookup;

    public InMemoryUnitCatalog()
    {
        _categories = SeedCategories();
        var allUnits = SeedUnits();

        _categoryLookup = new Dictionary<string, UnitCategory>(StringComparer.OrdinalIgnoreCase);
        foreach (var category in _categories)
        {
            _categoryLookup[category.Id] = category;
            _categoryLookup[category.Name] = category;
            foreach (var alias in category.Aliases)
            {
                _categoryLookup[alias] = category;
            }
        }

        _unitsByCategory = new Dictionary<string, List<UnitDefinition>>(StringComparer.OrdinalIgnoreCase);
        foreach (var category in _categories)
        {
            _unitsByCategory[category.Id] = [];
        }

        _unitLookup = new Dictionary<(string, string), UnitDefinition>(new CategoryAndKeyComparer());

        foreach (var unit in allUnits)
        {
            if (_unitsByCategory.TryGetValue(unit.CategoryId, out var list))
            {
                list.Add(unit);
            }

            // Register canonical ID
            RegisterLookup(unit.CategoryId, unit.Id, unit);
            // Register display Name
            RegisterLookup(unit.CategoryId, unit.Name, unit);
            // Register Symbol
            RegisterLookup(unit.CategoryId, unit.Symbol, unit);

            // Register all aliases
            foreach (var alias in unit.Aliases)
            {
                RegisterLookup(unit.CategoryId, alias, unit);
            }
        }
    }

    private void RegisterLookup(string categoryId, string key, UnitDefinition unit)
    {
        var lookupKey = (categoryId.Trim().ToLowerInvariant(), key.Trim().ToLowerInvariant());
        _unitLookup.TryAdd(lookupKey, unit);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<UnitCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<UnitCategory>>(_categories.AsReadOnly());
    }

    /// <inheritdoc />
    public Task<UnitCategory?> GetCategoryAsync(string categoryIdOrAlias, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(categoryIdOrAlias))
        {
            return Task.FromResult<UnitCategory?>(null);
        }

        _categoryLookup.TryGetValue(categoryIdOrAlias.Trim(), out var category);
        return Task.FromResult(category);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<UnitDefinition>> GetUnitsByCategoryAsync(string categoryIdOrAlias, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(categoryIdOrAlias))
        {
            return Task.FromResult<IReadOnlyList<UnitDefinition>>([]);
        }

        if (!_categoryLookup.TryGetValue(categoryIdOrAlias.Trim(), out var category))
        {
            return Task.FromResult<IReadOnlyList<UnitDefinition>>([]);
        }

        if (_unitsByCategory.TryGetValue(category.Id, out var units))
        {
            return Task.FromResult<IReadOnlyList<UnitDefinition>>(units.AsReadOnly());
        }

        return Task.FromResult<IReadOnlyList<UnitDefinition>>([]);
    }

    /// <inheritdoc />
    public Task<UnitDefinition?> FindUnitAsync(string categoryIdOrAlias, string unitNameOrSymbol, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(categoryIdOrAlias) || string.IsNullOrWhiteSpace(unitNameOrSymbol))
        {
            return Task.FromResult<UnitDefinition?>(null);
        }

        if (!_categoryLookup.TryGetValue(categoryIdOrAlias.Trim(), out var category))
        {
            return Task.FromResult<UnitDefinition?>(null);
        }

        var key = (category.Id.Trim().ToLowerInvariant(), unitNameOrSymbol.Trim().ToLowerInvariant());
        _unitLookup.TryGetValue(key, out var unit);

        return Task.FromResult(unit);
    }

    private static List<UnitCategory> SeedCategories() =>
    [
        new UnitCategory
        {
            Id = "length",
            Name = "Length",
            BaseUnitId = "meter",
            Description = "Distance and linear measurement units.",
            Aliases = ["lengths", "distance", "distances"]
        },
        new UnitCategory
        {
            Id = "temperature",
            Name = "Temperature",
            BaseUnitId = "kelvin",
            Description = "Thermodynamic temperature measurement units.",
            Aliases = ["temperatures", "temp"],
            // Absolute zero in Kelvin is 0 K.
            MinAllowedBaseValue = 0m
        },
        new UnitCategory
        {
            Id = "weight",
            Name = "Weight/Mass",
            BaseUnitId = "kilogram",
            Description = "Mass and weight measurement units.",
            Aliases = ["mass", "weights", "masses", "weight/mass", "weight-mass"]
        }
    ];

    private static List<UnitDefinition> SeedUnits()
    {
        // Fahrenheit affine parameters relative to Kelvin:
        // K = (F - 32) * (5/9) + 273.15
        // K = F * (5/9) + (273.15 - 32 * (5/9))
        const decimal fahrenheitFactor = 5m / 9m;
        const decimal fahrenheitOffset = 273.15m - (32m * (5m / 9m));

        return
        [
            // --- Length Units (Base: meter) ---
            new UnitDefinition
            {
                Id = "millimeter",
                Name = "Millimeter",
                Symbol = "mm",
                CategoryId = "length",
                Factor = 0.001m,
                Offset = 0m,
                Aliases = ["millimeter", "millimeters", "millimetre", "millimetres", "mm"]
            },
            new UnitDefinition
            {
                Id = "centimeter",
                Name = "Centimeter",
                Symbol = "cm",
                CategoryId = "length",
                Factor = 0.01m,
                Offset = 0m,
                Aliases = ["centimeter", "centimeters", "centimetre", "centimetres", "cm"]
            },
            new UnitDefinition
            {
                Id = "meter",
                Name = "Meter",
                Symbol = "m",
                CategoryId = "length",
                Factor = 1.0m,
                Offset = 0m,
                Aliases = ["meter", "meters", "metre", "metres", "m"]
            },
            new UnitDefinition
            {
                Id = "kilometer",
                Name = "Kilometer",
                Symbol = "km",
                CategoryId = "length",
                Factor = 1000.0m,
                Offset = 0m,
                Aliases = ["kilometer", "kilometers", "kilometre", "kilometres", "km"]
            },
            new UnitDefinition
            {
                Id = "inch",
                Name = "Inch",
                Symbol = "in",
                CategoryId = "length",
                Factor = 0.0254m,
                Offset = 0m,
                Aliases = ["inch", "inches", "in", "\""]
            },
            new UnitDefinition
            {
                Id = "foot",
                Name = "Foot",
                Symbol = "ft",
                CategoryId = "length",
                Factor = 0.3048m,
                Offset = 0m,
                Aliases = ["foot", "feet", "ft", "'"]
            },
            new UnitDefinition
            {
                Id = "yard",
                Name = "Yard",
                Symbol = "yd",
                CategoryId = "length",
                Factor = 0.9144m,
                Offset = 0m,
                Aliases = ["yard", "yards", "yd"]
            },
            new UnitDefinition
            {
                Id = "mile",
                Name = "Mile",
                Symbol = "mi",
                CategoryId = "length",
                Factor = 1609.344m,
                Offset = 0m,
                Aliases = ["mile", "miles", "mi"]
            },

            // --- Temperature Units (Base: kelvin, Absolute zero: 0 K) ---
            new UnitDefinition
            {
                Id = "kelvin",
                Name = "Kelvin",
                Symbol = "K",
                CategoryId = "temperature",
                Factor = 1.0m,
                Offset = 0m,
                Aliases = ["kelvin", "kelvins", "k", "deg k", "degree kelvin", "degrees kelvin"]
            },
            new UnitDefinition
            {
                Id = "celsius",
                Name = "Celsius",
                Symbol = "°C",
                CategoryId = "temperature",
                Factor = 1.0m,
                Offset = 273.15m,
                Aliases = ["celsius", "c", "°c", "deg c", "degree celsius", "degrees celsius", "centigrade"]
            },
            new UnitDefinition
            {
                Id = "fahrenheit",
                Name = "Fahrenheit",
                Symbol = "°F",
                CategoryId = "temperature",
                Factor = fahrenheitFactor,
                Offset = fahrenheitOffset,
                Aliases = ["fahrenheit", "f", "°f", "deg f", "degree fahrenheit", "degrees fahrenheit"]
            },

            // --- Weight / Mass Units (Base: kilogram) ---
            new UnitDefinition
            {
                Id = "milligram",
                Name = "Milligram",
                Symbol = "mg",
                CategoryId = "weight",
                Factor = 0.000001m,
                Offset = 0m,
                Aliases = ["milligram", "milligrams", "mg"]
            },
            new UnitDefinition
            {
                Id = "gram",
                Name = "Gram",
                Symbol = "g",
                CategoryId = "weight",
                Factor = 0.001m,
                Offset = 0m,
                Aliases = ["gram", "grams", "g"]
            },
            new UnitDefinition
            {
                Id = "kilogram",
                Name = "Kilogram",
                Symbol = "kg",
                CategoryId = "weight",
                Factor = 1.0m,
                Offset = 0m,
                Aliases = ["kilogram", "kilograms", "kg", "kilo", "kilos"]
            },
            new UnitDefinition
            {
                Id = "tonne",
                Name = "Tonne",
                Symbol = "t",
                CategoryId = "weight",
                Factor = 1000.0m,
                Offset = 0m,
                Aliases = ["tonne", "tonnes", "t", "metric ton", "metric tons", "metric-tonne"]
            },
            new UnitDefinition
            {
                Id = "ounce",
                Name = "Ounce",
                Symbol = "oz",
                CategoryId = "weight",
                Factor = 0.028349523125m,
                Offset = 0m,
                Aliases = ["ounce", "ounces", "oz"]
            },
            new UnitDefinition
            {
                Id = "pound",
                Name = "Pound",
                Symbol = "lb",
                CategoryId = "weight",
                Factor = 0.45359237m,
                Offset = 0m,
                Aliases = ["pound", "pounds", "lb", "lbs"]
            },
            new UnitDefinition
            {
                Id = "stone",
                Name = "Stone",
                Symbol = "st",
                CategoryId = "weight",
                Factor = 6.35029318m,
                Offset = 0m,
                Aliases = ["stone", "stones", "st"]
            }
        ];
    }

    private sealed class CategoryAndKeyComparer : IEqualityComparer<(string CategoryId, string LookupKey)>
    {
        public bool Equals((string CategoryId, string LookupKey) x, (string CategoryId, string LookupKey) y)
        {
            return string.Equals(x.CategoryId, y.CategoryId, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(x.LookupKey, y.LookupKey, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode((string CategoryId, string LookupKey) obj)
        {
            return HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.CategoryId),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.LookupKey));
        }
    }
}
