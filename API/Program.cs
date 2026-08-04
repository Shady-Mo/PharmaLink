using Hangfire;
using Application.Hubs;
using Application.Services.AI;
using API.Notification;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApiServices()
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration);

builder.Services.AddHealthChecks();
builder.Services.AddSignalR();

builder.Services.AddScoped<INotificationService, NotificationService>();

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    recurringJobManager.AddOrUpdate<IInventoryForecastingBackgroundJob>(
        "inventory-forecasting-daily-job",
        job => job.RunDailyForecastAsync(),
        Cron.Yearly
    );
}

app.UseSwaggerDocs();

app.UseScalarDocs();

app.UseHttpsRedirection();

app.UseDefaultFiles();

app.UseStaticFiles();

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();

app.UseHangfireDashboard();

app.MapControllers();

app.MapHealthChecks("/health");

app.MapHub<InventoryHub>("/inventory-hub");

app.Run();