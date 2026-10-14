using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using UnitConversion.Application.Configuration;
using UnitConversion.Application.DTOs;
using UnitConversion.Application.Services;
using UnitConversion.Domain.Exceptions;
using UnitConversion.Infrastructure.Catalog;
using Xunit;

namespace UnitConversion.Tests.Unit;

public class ConversionServiceTests
{
    private readonly ConversionService _sut;

    public ConversionServiceTests()
    {
        var catalog = new InMemoryUnitCatalog();
        var options = Options.Create(new ConversionOptions
        {
            DefaultPrecision = 4,
            MaxPrecision = 15
        });
        var logger = NullLogger<ConversionService>.Instance;

        _sut = new ConversionService(catalog, options, logger);
    }

    [Fact]
    public async Task Convert_MeterToFoot_MatchesSampleSpecification()
    {
        // Arrange
        var request = new ConversionRequestDto
        {
            Category = "length",
            FromUnit = "meter",
            ToUnit = "foot",
            Value = 10m
        };

        // Act
        var response = await _sut.ConvertAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Category.Should().Be("length");
        response.FromUnit.Should().Be("meter");
        response.ToUnit.Should().Be("foot");
        response.InputValue.Should().Be(10m);
        response.Result.Should().Be(32.8084m);
    }

    [Theory]
    [InlineData("millimeter", 1000, "meter", 1.0)]
    [InlineData("centimeter", 100, "meter", 1.0)]
    [InlineData("kilometer", 1, "meter", 1000.0)]
    [InlineData("inch", 1, "centimeter", 2.54)]
    [InlineData("foot", 1, "inch", 12.0)]
    [InlineData("yard", 1, "foot", 3.0)]
    [InlineData("mile", 1, "kilometer", 1.6093)]
    public async Task Convert_LengthUnits_CalculatesCorrectly(string from, decimal val, string to, decimal expected)
    {
        var request = new ConversionRequestDto
        {
            Category = "length",
            FromUnit = from,
            ToUnit = to,
            Value = val
        };

        var response = await _sut.ConvertAsync(request);

        response.Result.Should().Be(expected);
    }

    [Theory]
    [InlineData("celsius", 0, "fahrenheit", 32.0)]
    [InlineData("celsius", 100, "fahrenheit", 212.0)]
    [InlineData("celsius", -40, "fahrenheit", -40.0)]
    [InlineData("fahrenheit", 32, "celsius", 0.0)]
    [InlineData("fahrenheit", 212, "celsius", 100.0)]
    [InlineData("fahrenheit", -40, "celsius", -40.0)]
    [InlineData("celsius", 25, "kelvin", 298.15)]
    [InlineData("kelvin", 0, "celsius", -273.15)]
    [InlineData("kelvin", 0, "fahrenheit", -459.67)]
    public async Task Convert_TemperatureUnits_CalculatesCorrectly(string from, decimal val, string to, decimal expected)
    {
        var request = new ConversionRequestDto
        {
            Category = "temperature",
            FromUnit = from,
            ToUnit = to,
            Value = val
        };

        var response = await _sut.ConvertAsync(request);

        response.Result.Should().Be(expected);
    }

    [Theory]
    [InlineData("gram", 1000, "kilogram", 1.0)]
    [InlineData("milligram", 1000000, "kilogram", 1.0)]
    [InlineData("tonne", 1, "kilogram", 1000.0)]
    [InlineData("pound", 1, "ounce", 16.0)]
    [InlineData("stone", 1, "pound", 14.0)]
    [InlineData("kilogram", 10, "pound", 22.0462)]
    public async Task Convert_WeightUnits_CalculatesCorrectly(string from, decimal val, string to, decimal expected)
    {
        var request = new ConversionRequestDto
        {
            Category = "weight",
            FromUnit = from,
            ToUnit = to,
            Value = val
        };

        var response = await _sut.ConvertAsync(request);

        response.Result.Should().Be(expected);
    }

    [Fact]
    public async Task Convert_WeightCategoryAlias_Mass_Succeeds()
    {
        var request = new ConversionRequestDto
        {
            Category = "mass",
            FromUnit = "kg",
            ToUnit = "lb",
            Value = 5m
        };

        var response = await _sut.ConvertAsync(request);

        response.Category.Should().Be("weight");
        response.Result.Should().Be(11.0231m);
    }

    [Theory]
    [InlineData("length", "m", "FT", 10, 32.8084)]
    [InlineData("Length", "METERS", "feet", 10, 32.8084)]
    [InlineData("temperature", "°C", "°F", 100, 212)]
    [InlineData("Temperature", "c", "f", 0, 32)]
    [InlineData("weight", "KG", "LBS", 1, 2.2046)]
    public async Task Convert_CaseInsensitiveAndSymbols_Succeeds(
        string category, string from, string to, decimal val, decimal expected)
    {
        var request = new ConversionRequestDto
        {
            Category = category,
            FromUnit = from,
            ToUnit = to,
            Value = val
        };

        var response = await _sut.ConvertAsync(request);

        response.Result.Should().Be(expected);
    }

    [Theory]
    [InlineData("length", "meter", "foot")]
    [InlineData("temperature", "celsius", "fahrenheit")]
    [InlineData("weight", "kilogram", "pound")]
    public async Task Convert_RoundTrip_PreservesValueAccurately(string category, string unitA, string unitB)
    {
        var initialValue = 42.5m;

        // Forward
        var forward = await _sut.ConvertAsync(new ConversionRequestDto
        {
            Category = category,
            FromUnit = unitA,
            ToUnit = unitB,
            Value = initialValue,
            Precision = 10
        });

        // Reverse
        var reverse = await _sut.ConvertAsync(new ConversionRequestDto
        {
            Category = category,
            FromUnit = unitB,
            ToUnit = unitA,
            Value = forward.Result,
            Precision = 4
        });

        reverse.Result.Should().Be(initialValue);
    }

    [Theory]
    [InlineData("length", "meter", 123.456)]
    [InlineData("temperature", "celsius", -15.5)]
    [InlineData("weight", "kilogram", 88.0)]
    public async Task Convert_SameUnit_ReturnsIdenticalValueWithoutDrift(string category, string unit, decimal value)
    {
        var request = new ConversionRequestDto
        {
            Category = category,
            FromUnit = unit,
            ToUnit = unit,
            Value = value
        };

        var response = await _sut.ConvertAsync(request);

        response.InputValue.Should().Be(value);
        response.Result.Should().Be(value);
    }

    [Fact]
    public async Task Convert_ZeroValue_ConvertsCorrectly()
    {
        var request = new ConversionRequestDto
        {
            Category = "length",
            FromUnit = "meter",
            ToUnit = "foot",
            Value = 0m
        };

        var response = await _sut.ConvertAsync(request);

        response.Result.Should().Be(0m);
    }

    [Fact]
    public async Task Convert_NegativeValueForLength_ConvertsMathematically()
    {
        var request = new ConversionRequestDto
        {
            Category = "length",
            FromUnit = "meter",
            ToUnit = "foot",
            Value = -10m
        };

        var response = await _sut.ConvertAsync(request);

        response.Result.Should().Be(-32.8084m);
    }

    [Fact]
    public async Task Convert_AbsoluteZero_AllowedAtBoundary()
    {
        var requestKelvin = new ConversionRequestDto
        {
            Category = "temperature",
            FromUnit = "kelvin",
            ToUnit = "celsius",
            Value = 0m
        };

        var response = await _sut.ConvertAsync(requestKelvin);
        response.Result.Should().Be(-273.15m);
    }

    [Theory]
    [InlineData("kelvin", -0.0001)]
    [InlineData("celsius", -273.16)]
    [InlineData("fahrenheit", -460.0)]
    public async Task Convert_BelowAbsoluteZero_ThrowsUnitConversionValidationException(string unit, decimal value)
    {
        var request = new ConversionRequestDto
        {
            Category = "temperature",
            FromUnit = unit,
            ToUnit = "celsius",
            Value = value
        };

        var act = async () => await _sut.ConvertAsync(request);

        var exception = await act.Should().ThrowAsync<UnitConversionValidationException>();
        exception.Which.Message.Should().Contain("below absolute zero");
    }

    [Fact]
    public async Task Convert_UnknownCategory_ThrowsCategoryNotFoundException()
    {
        var request = new ConversionRequestDto
        {
            Category = "speed",
            FromUnit = "mph",
            ToUnit = "kph",
            Value = 100m
        };

        var act = async () => await _sut.ConvertAsync(request);

        var exception = await act.Should().ThrowAsync<CategoryNotFoundException>();
        exception.Which.Category.Should().Be("speed");
    }

    [Fact]
    public async Task Convert_UnitNotInSpecifiedCategory_ThrowsUnitNotFoundException()
    {
        var request = new ConversionRequestDto
        {
            Category = "length",
            FromUnit = "celsius",
            ToUnit = "meter",
            Value = 10m
        };

        var act = async () => await _sut.ConvertAsync(request);

        var exception = await act.Should().ThrowAsync<UnitNotFoundException>();
        exception.Which.Unit.Should().Be("celsius");
        exception.Which.Category.Should().Be("length");
    }

    [Fact]
    public async Task Convert_CustomPrecision_RoundsToRequestedPlaces()
    {
        var request = new ConversionRequestDto
        {
            Category = "length",
            FromUnit = "meter",
            ToUnit = "foot",
            Value = 10m,
            Precision = 2
        };

        var response = await _sut.ConvertAsync(request);

        response.Result.Should().Be(32.81m);
    }
}
