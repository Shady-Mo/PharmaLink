namespace API;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers();

        services.AddOpenApi();

        services.AddScoped<IDrugService, DrugService>();

        return services;
    }
}