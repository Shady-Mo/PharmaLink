using Microsoft.Extensions.Caching.Distributed;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApiServices()
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration);

builder.Services.AddHealthChecks();

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
    try {
        await cache.SetStringAsync("TestKey", "Redis_Is_Connected", new DistributedCacheEntryOptions {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
        });

        var value = await cache.GetStringAsync("TestKey");
        Console.WriteLine($"[Redis Success]: Connected successfully!\n");
    }
    catch (Exception ex) {
        Console.WriteLine($"[Redis Error]: Connection failed!\n");
    }
}

app.UseSwaggerDocs();

app.UseScalarDocs();

app.UseHttpsRedirection();

app.UseDefaultFiles();

app.UseStaticFiles();

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();