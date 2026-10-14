using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UnitConversion.Application.Configuration;
using UnitConversion.Application.Interfaces;
using UnitConversion.Application.Services;

namespace UnitConversion.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ConversionOptions>(configuration.GetSection(ConversionOptions.SectionName));
        services.AddScoped<IConversionService, ConversionService>();

        return services;
    }
}
