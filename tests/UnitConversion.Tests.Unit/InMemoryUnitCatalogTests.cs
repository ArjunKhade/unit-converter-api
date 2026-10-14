using FluentAssertions;
using UnitConversion.Infrastructure.Catalog;
using Xunit;

namespace UnitConversion.Tests.Unit;

public class InMemoryUnitCatalogTests
{
    private readonly InMemoryUnitCatalog _sut = new();

    [Fact]
    public async Task GetCategoriesAsync_ReturnsAllConfiguredCategories()
    {
        var categories = await _sut.GetCategoriesAsync();

        categories.Should().HaveCount(3);
        categories.Select(c => c.Id).Should().Contain(["length", "temperature", "weight"]);
    }

    [Theory]
    [InlineData("length", "Length")]
    [InlineData("LENGTH", "Length")]
    [InlineData("temperature", "Temperature")]
    [InlineData("weight", "Weight/Mass")]
    [InlineData("mass", "Weight/Mass")] // Alias
    public async Task GetCategoryAsync_FindsByIdOrAlias_CaseInsensitive(string input, string expectedName)
    {
        var category = await _sut.GetCategoryAsync(input);

        category.Should().NotBeNull();
        category!.Name.Should().Be(expectedName);
    }

    [Fact]
    public async Task GetUnitsByCategoryAsync_Length_ContainsMinimumRequiredUnits()
    {
        var units = await _sut.GetUnitsByCategoryAsync("length");

        var unitIds = units.Select(u => u.Id).ToList();
        unitIds.Should().Contain([
            "millimeter", "centimeter", "meter", "kilometer", "inch", "foot", "yard", "mile"
        ]);
    }

    [Theory]
    [InlineData("length", "meter", "meter")]
    [InlineData("length", "m", "meter")]
    [InlineData("length", "Meters", "meter")]
    [InlineData("length", "ft", "foot")]
    [InlineData("temperature", "celsius", "celsius")]
    [InlineData("temperature", "°C", "celsius")]
    [InlineData("temperature", "C", "celsius")]
    [InlineData("temperature", "°F", "fahrenheit")]
    [InlineData("temperature", "K", "kelvin")]
    [InlineData("weight", "kg", "kilogram")]
    [InlineData("weight", "pounds", "pound")]
    [InlineData("weight", "lb", "pound")]
    [InlineData("mass", "st", "stone")]
    public async Task FindUnitAsync_ResolvesBySymbolNameOrAlias(string category, string query, string expectedUnitId)
    {
        var unit = await _sut.FindUnitAsync(category, query);

        unit.Should().NotBeNull();
        unit!.Id.Should().Be(expectedUnitId);
    }
}
