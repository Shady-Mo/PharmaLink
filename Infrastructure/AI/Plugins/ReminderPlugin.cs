using System.ComponentModel;
using Application.DTOs.MedicineReminder;
using Application.Services;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.AI.Plugins;

/// <summary>
/// Native SK plugin that allows the AI to manage patient medicine reminders.
/// </summary>
public sealed class ReminderPlugin(IServiceScopeFactory scopeFactory, ILogger<ReminderPlugin> logger)
{
    [KernelFunction("create_dosage_schedule")]
    [Description(
        "Creates a dosage schedule/reminder for a patient's medication. " +
        "Use this when a user asks to be reminded to take their medicine or asks you to organize their dosage schedule.")]
    public async Task<object> CreateDosageScheduleAsync(
        [Description("The name of the medicine (e.g., 'Panadol')")] string medicineName,
        [Description("The dosage instructions (e.g., '1 tablet', '5ml')")] string dosage,
        [Description("Any additional notes or instructions (e.g., 'after meals')")] string notes,
        [Description("A comma-separated list of times in HH:mm 24-hour format (e.g., '08:00,20:00' for every 12 hours)")] string reminderTimes,
        [Description("The duration of the treatment in days. If the user doesn't specify, default to 7.")] int durationDays,
        [Description("The patient's user ID")] Guid patientUserId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("ReminderPlugin.CreateDosageScheduleAsync called for patient: {PatientId}, Medicine: {Medicine}", patientUserId, medicineName);

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var reminderService = scope.ServiceProvider.GetRequiredService<IMedicineReminderService>();

            var timeList = reminderTimes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                        .Select(t => t.Trim())
                                        .ToList();

            var startDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var endDate = startDate.AddDays(durationDays);

            var request = new CreateReminderRequest
            {
                MedicineName = medicineName,
                Dosage = dosage,
                Notes = notes,
                ReminderTimes = timeList,
                StartDate = startDate,
                EndDate = endDate,
                NotifyByEmail = true,
                NotifyByWhatsApp = false
            };

            var result = await reminderService.CreateAsync(patientUserId, request);

            if (result.IsSuccess)
            {
                return new { Success = true, Message = "Dosage schedule created successfully.", ReminderDetails = result.Value };
            }

            return new { Success = false, Error = result.Error.Description };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create dosage schedule in ReminderPlugin.");
            return new { Success = false, Error = "An error occurred while creating the dosage schedule." };
        }
    }
}
