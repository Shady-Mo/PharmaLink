using API.Hubs;
using API.Middlewares;
using API.Notification;
using API.Services;
using Application.Hubs;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddScoped<IReminderPushService, SignalRReminderPushService>();

builder.Services.AddHealthChecks();
builder.Services.AddSignalR();

builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDeliveryNotificationService, DeliveryNotificationService>();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var patientInitializer = scope.ServiceProvider.GetRequiredService<Infrastructure.AI.PatientPrescriptionCollectionInitializer>();
        await patientInitializer.InitializeAsync();

        var analyticsInitializer = scope.ServiceProvider.GetRequiredService<Infrastructure.AI.PrescriptionAnalyticsCollectionInitializer>();
        await analyticsInitializer.InitializeAsync();
    }
    catch (Exception ex)
    {
        var startupLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Startup");
        startupLogger.LogError(ex,
            "Failed to initialize Qdrant collections. Prescription search and analytics RAG may be degraded.");
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

app.UseHangfireDashboard();

// Register recurring Hangfire jobs
RecurringJob.AddOrUpdate<IMedicineReminderService>(
    "medicine-reminders-every-minute",
    job => job.ProcessDueRemindersAsync(),
    Cron.Minutely);

RecurringJob.AddOrUpdate<IRecurringPrescriptionService>(
    "recurring-prescriptions-daily",
    job => job.ProcessDueRecurringAsync(),
    "0 8 * * *"); // 8 AM daily

RecurringJob.AddOrUpdate<IRecurringPrescriptionService>(
    "recurring-prescriptions-auto-confirm",
    job => job.AutoConfirmExpiredRunsAsync(),
    "0 */6 * * *"); // every 6 hours


app.MapControllers();

app.MapHealthChecks("/health");

app.MapHub<InventoryHub>("/inventory-hub");
app.MapHub<DeliveryHub>("/hubs/delivery");
app.MapHub<MedicineReminderHub>("/hubs/reminders");

app.Run();