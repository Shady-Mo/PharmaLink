namespace API.Extensions;

public static class ScalarExtensions
{
    public static WebApplication UseScalarDocs(this WebApplication app)
    {
        app.MapScalarApiReference("/docs", options =>
        {
            options
                .WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json")
                .WithTitle("Pharma Link API Reference")
                .WithTheme(ScalarTheme.BluePlanet)
                .SortTagsAlphabetically()
                .AlwaysShowDeveloperTools()
                .HideModels = false;

            options.AddDocument("v1", "Pharma Link v1",
                $"/swagger/v1/swagger.json");

            options.AddPreferredSecuritySchemes("bearer")
                .AddHttpAuthentication("bearer", auth =>
                {
                    auth.Token = "{your JWT token}";
                    auth.Description = "JWT Authorization header using the Bearer scheme.";
                });
        });

        return app;
    }
}