using API.Hubs;
using API.Middlewares;
using API.Notification;
using API.Services;
using Application.Abstractions;
using Application.Hubs;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddScoped<IReminderPushService, SignalRReminderPushService>();
builder.Services.AddScoped<ILiveNotificationService, LiveNotificationService>();

builder.Services.AddHealthChecks();
builder.Services.AddSignalR();

builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDeliveryNotificationService, DeliveryNotificationService>();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

//using (var scope = app.Services.CreateScope())
//{
//    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

//    try
//    {
//        var initializer = scope.ServiceProvider.GetRequiredService<PatientPrescriptionCollectionInitializer>();
//        await initializer.InitializeAsync();
//    }
//    catch (Exception ex)
//    {
//        var startupLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
//            .CreateLogger("Startup");
//        startupLogger.LogError(ex,
//            "Failed to initialize Qdrant 'patient_prescriptions' collection. " +
//            "Prescription search will be degraded until Qdrant is reachable.");
//    }

//    recurringJobManager.AddOrUpdate<IInventoryForecastingBackgroundJob>(
//        "inventory-forecasting-daily-job",
//        job => job.RunDailyForecastAsync(),
//        Cron.Daily
//    );
//}

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
    "0 8 * * *"); 

RecurringJob.AddOrUpdate<IRecurringPrescriptionService>(
    "recurring-prescriptions-auto-confirm",
    job => job.AutoConfirmExpiredRunsAsync(),
    "0 */6 * * *"); 

app.MapControllers();

app.MapHealthChecks("/health");

app.MapHub<InventoryHub>("/inventory-hub");
app.MapHub<DeliveryHub>("/hubs/delivery");
app.MapHub<MedicineReminderHub>("/hubs/reminders");
app.MapHub<NotificationHub>("/hubs/notification");

app.Run();