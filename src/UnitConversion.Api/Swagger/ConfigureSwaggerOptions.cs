using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace UnitConversion.Api.Swagger;

/// <summary>
/// Configures Swagger options per discovered API version.
/// </summary>
public sealed class ConfigureSwaggerOptions : IConfigureNamedOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            var openApiInfo = new Microsoft.OpenApi.OpenApiInfo
            {
                Title = "Unit Conversion API",
                Version = description.ApiVersion.ToString(),
                Description = "A production-grade RESTful API for converting numerical values across measurement units with affine modeling."
            };

            if (description.IsDeprecated)
            {
                openApiInfo.Description += " (This API version has been deprecated).";
            }

            options.SwaggerDoc(description.GroupName, openApiInfo);
        }
    }

    public void Configure(string? name, SwaggerGenOptions options) => Configure(options);
}
