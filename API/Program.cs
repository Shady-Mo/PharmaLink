using API.Notification;
using Application.Hubs;
using Application.Services.AI;
using Hangfire;
using Infrastructure.AI;

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
    var initializer = scope.ServiceProvider.GetRequiredService<PatientPrescriptionCollectionInitializer>();
    recurringJobManager.AddOrUpdate<IInventoryForecastingBackgroundJob>(
        "inventory-forecasting-daily-job",
        job => job.RunDailyForecastAsync(),
        Cron.Daily
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