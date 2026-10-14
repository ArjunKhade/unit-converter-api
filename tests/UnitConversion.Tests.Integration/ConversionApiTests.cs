using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using UnitConversion.Application.DTOs;
using Xunit;

namespace UnitConversion.Tests.Integration;

public class ConversionApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ConversionApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostConversion_ValidMeterToFoot_ReturnsExpectedResult()
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
        var response = await _client.PostAsJsonAsync("/api/v1/conversions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ConversionResponseDto>(JsonOptions);
        content.Should().NotBeNull();
        content!.Category.Should().Be("length");
        content.FromUnit.Should().Be("meter");
        content.ToUnit.Should().Be("foot");
        content.InputValue.Should().Be(10m);
        content.Result.Should().Be(32.8084m);
    }

    [Fact]
    public async Task PostConversion_CelsiusToFahrenheit_ReturnsExpectedResult()
    {
        var request = new ConversionRequestDto
        {
            Category = "temperature",
            FromUnit = "celsius",
            ToUnit = "fahrenheit",
            Value = 100m
        };

        var response = await _client.PostAsJsonAsync("/api/v1/conversions", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ConversionResponseDto>(JsonOptions);
        content.Should().NotBeNull();
        content!.Result.Should().Be(212.0m);
    }

    [Fact]
    public async Task PostConversion_WithUnitSymbols_ReturnsSuccess()
    {
        var request = new ConversionRequestDto
        {
            Category = "length",
            FromUnit = "m",
            ToUnit = "ft",
            Value = 10m
        };

        var response = await _client.PostAsJsonAsync("/api/v1/conversions", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ConversionResponseDto>(JsonOptions);
        content.Should().NotBeNull();
        content!.Result.Should().Be(32.8084m);
    }

    [Fact]
    public async Task PostConversion_BelowAbsoluteZero_ReturnsBadRequestProblemDetails()
    {
        var request = new ConversionRequestDto
        {
            Category = "temperature",
            FromUnit = "celsius",
            ToUnit = "kelvin",
            Value = -300m // Below -273.15 °C
        };

        var response = await _client.PostAsJsonAsync("/api/v1/conversions", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Detail.Should().Contain("below absolute zero");
    }

    [Fact]
    public async Task PostConversion_UnknownCategory_ReturnsNotFoundProblemDetails()
    {
        var request = new ConversionRequestDto
        {
            Category = "speed",
            FromUnit = "mph",
            ToUnit = "kmh",
            Value = 60m
        };

        var response = await _client.PostAsJsonAsync("/api/v1/conversions", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task PostConversion_UnknownUnit_ReturnsBadRequestProblemDetails()
    {
        var request = new ConversionRequestDto
        {
            Category = "length",
            FromUnit = "lightyear",
            ToUnit = "meter",
            Value = 1m
        };

        var response = await _client.PostAsJsonAsync("/api/v1/conversions", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task GetCategories_ReturnsListWithExpectedCategories()
    {
        var response = await _client.GetAsync("/api/v1/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>(JsonOptions);
        categories.Should().NotBeNull();
        categories.Should().HaveCount(3);
        categories!.Select(c => c.Id).Should().Contain(["length", "temperature", "weight"]);
    }

    [Fact]
    public async Task GetCategoryUnits_ExistingCategory_ReturnsUnitsListWithSymbols()
    {
        var response = await _client.GetAsync("/api/v1/categories/length/units");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var units = await response.Content.ReadFromJsonAsync<List<UnitDto>>(JsonOptions);
        units.Should().NotBeNull();
        units!.Should().NotBeEmpty();
        units.Should().Contain(u => u.Id == "meter" && u.Symbol == "m");
    }

    [Fact]
    public async Task GetCategoryUnits_UnknownCategory_ReturnsNotFoundProblemDetails()
    {
        var response = await _client.GetAsync("/api/v1/categories/invalid_category/units");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthyStatus()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Healthy");
    }
}
