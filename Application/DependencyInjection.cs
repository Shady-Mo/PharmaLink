using Application.Services;
using Infrastructure.Services;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddMapster();
        services.AddScoped<IInventoryForecastingCalculator, InventoryForecastingCalculator>();
        services.AddScoped<IInventoryForecastingBackgroundJob, InventoryForecastingBackgroundJob>();

        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(Assembly.GetExecutingAssembly());

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}