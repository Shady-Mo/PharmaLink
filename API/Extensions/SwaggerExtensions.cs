namespace API.Extensions;

public static class SwaggerExtensions
{
    public static WebApplication UseSwaggerDocs(this WebApplication app)
    {
        app.UseSwagger();

        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "PharmaLink API v1");

            c.RoutePrefix = string.Empty;

            c.DisplayRequestDuration();
        });

        return app;
    }
}