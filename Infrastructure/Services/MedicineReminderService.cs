using Application.DTOs.MedicineReminder;

namespace Infrastructure.Services;

public class MedicineReminderService(
    AppDbContext context,
    IEmailService emailService,
    IWhatsAppMessageService whatsAppService,
    IReminderPushService reminderPush,
    ILogger<MedicineReminderService> logger) : IMedicineReminderService
{
    public async Task<Result<ReminderResponseDto>> CreateAsync(Guid patientId, CreateReminderRequest request)
    {
        var reminder = new MedicineReminder
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            MedicineName = request.MedicineName,
            Dosage = request.Dosage,
            Notes = request.Notes,
            ReminderTimesJson = JsonSerializer.Serialize(request.ReminderTimes),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PrescriptionReviewMedicineId = request.PrescriptionReviewMedicineId,
            NotifyByEmail = request.NotifyByEmail,
            NotifyByWhatsApp = request.NotifyByWhatsApp,
        };

        context.MedicineReminders.Add(reminder);
        await context.SaveChangesAsync();
        return Result.Success(MapToDto(reminder));
    }

    public async Task<Result<ReminderResponseDto>> UpdateAsync(Guid id, Guid patientId, CreateReminderRequest request)
    {
        var reminder = await context.MedicineReminders
            .FirstOrDefaultAsync(r => r.Id == id && r.PatientId == patientId);
        if (reminder is null)
            return Result.Failure<ReminderResponseDto>(new Error("Reminder.NotFound",
                $"Reminder with id {id} not found.", 404));

        reminder.MedicineName = request.MedicineName;
        reminder.Dosage = request.Dosage;
        reminder.Notes = request.Notes;
        reminder.ReminderTimesJson = JsonSerializer.Serialize(request.ReminderTimes);
        reminder.StartDate = request.StartDate;
        reminder.EndDate = request.EndDate;
        reminder.NotifyByEmail = request.NotifyByEmail;
        reminder.NotifyByWhatsApp = request.NotifyByWhatsApp;

        await context.SaveChangesAsync();
        return Result.Success(MapToDto(reminder));
    }

    public async Task<Result> DeleteAsync(Guid id, Guid patientId)
    {
        var reminder = await context.MedicineReminders
            .FirstOrDefaultAsync(r => r.Id == id && r.PatientId == patientId);
        if (reminder is null)
            return Result.Failure(new Error("Reminder.NotFound", $"Reminder with id {id} not found.", 404));
        context.MedicineReminders.Remove(reminder);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ToggleAsync(Guid id, Guid patientId)
    {
        var reminder = await context.MedicineReminders
            .FirstOrDefaultAsync(r => r.Id == id && r.PatientId == patientId);
        if (reminder is null)
            return Result.Failure(new Error("Reminder.NotFound", $"Reminder with id {id} not found.", 404));
        reminder.IsActive = !reminder.IsActive;
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<List<ReminderResponseDto>>> GetPatientRemindersAsync(Guid patientId)
    {
        var reminders = await context.MedicineReminders
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return Result.Success(reminders.Select(MapToDto).ToList());
    }

    public async Task ProcessDueRemindersAsync()
    {
        var utcNow = DateTime.UtcNow;

        TimeZoneInfo egyptTimeZone;
        try
        {
            egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time"); // Windows
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo"); // Linux / macOS
            }
            catch (TimeZoneNotFoundException)
            {
                egyptTimeZone = TimeZoneInfo.Local; // Fallback
            }
        }

        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, egyptTimeZone);
        var today = DateOnly.FromDateTime(localNow);

        logger.LogInformation("--- START ProcessDueRemindersAsync ---");
        logger.LogInformation("UTC Now: {UtcNow}", utcNow);
        logger.LogInformation("Egypt Local Now: {LocalNow}", localNow);

        var reminders = await context.MedicineReminders
            .Include(r => r.Patient)
            .Where(r => r.IsActive
                        && r.StartDate <= today
                        && (r.EndDate == null || r.EndDate >= today))
            .ToListAsync();

        logger.LogInformation("Found {Count} active reminders for today.", reminders.Count);

        foreach (var reminder in reminders)
        {
            var times = JsonSerializer.Deserialize<List<string>>(reminder.ReminderTimesJson) ?? new();
            logger.LogInformation("Checking Reminder {Id} ({Name}) - Times: {TimesJson}", reminder.Id,
                reminder.MedicineName, reminder.ReminderTimesJson);

            foreach (var timeStr in times)
            {
                if (!TimeOnly.TryParse(timeStr, out var reminderTime))
                {
                    logger.LogWarning("Reminder {Id}: Could not parse time '{TimeStr}'", reminder.Id, timeStr);
                    continue;
                }

                // Construct exact local DateTime for this reminder today
                var reminderDateTimeLocal = localNow.Date + reminderTime.ToTimeSpan();

                // If it's in the future, it's not due yet
                if (localNow < reminderDateTimeLocal)
                {
                    continue;
                }

                // If we missed it by more than 45 minutes (e.g. server was down), skip it to avoid spam
                if (localNow - reminderDateTimeLocal > TimeSpan.FromMinutes(45))
                {
                    continue;
                }

                logger.LogInformation("Reminder Time {TimeStr} is DUE! (Local Reminder Time: {ReminderDateTimeLocal})",
                    timeStr, reminderDateTimeLocal);

                var reminderDateTimeUtc = TimeZoneInfo.ConvertTimeToUtc(reminderDateTimeLocal, egyptTimeZone);

                // Check if already sent for this specific occurrence (within a logical timeframe around the reminder time)
                var alreadySent = await context.MedicineReminderLogs
                    .AnyAsync(l => l.ReminderId == reminder.Id
                                   && l.SentAt >= reminderDateTimeUtc.AddMinutes(-5)
                                   && l.SentAt <= reminderDateTimeUtc.AddMinutes(50)
                                   && l.IsSuccess);

                if (alreadySent)
                {
                    logger.LogInformation(
                        "Reminder {Id}: Already sent successfully for {TimeStr} occurrence. Skipping.", reminder.Id,
                        timeStr);
                    continue;
                }

                logger.LogInformation("Reminder {Id}: Ready to send! Patient: {PatientId}", reminder.Id,
                    reminder.PatientId);
                var patient = reminder.Patient;

                if (reminder.NotifyByEmail && !string.IsNullOrEmpty(patient.Email))
                {
                    logger.LogInformation("Reminder {Id}: Sending Email to {Email}", reminder.Id, patient.Email);
                    await TrySendEmail(reminder, patient.Email, utcNow);
                }

                if (reminder.NotifyByWhatsApp && !string.IsNullOrEmpty(patient.PhoneNumber))
                {
                    logger.LogInformation("Reminder {Id}: Sending WhatsApp to {Phone}", reminder.Id,
                        patient.PhoneNumber);
                    await TrySendWhatsApp(reminder, patient.PhoneNumber, utcNow);
                }

                logger.LogInformation("Sending SignalR notification to Patient_{PatientId}", reminder.PatientId);
                try
                {
                    // SignalR — push to patient's browser immediately
                    await reminderPush.PushReminderAsync(
                        reminder.PatientId,
                        reminder.MedicineName,
                        reminder.Dosage,
                        reminder.Notes,
                        timeStr);
                    logger.LogInformation("SignalR notification sent successfully");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send SignalR notification for reminder {Id}", reminder.Id);
                }
            }
        }

        logger.LogInformation("--- END ProcessDueRemindersAsync ---");
    }

    private async Task TrySendEmail(MedicineReminder reminder, string email, DateTime now)
    {
        try
        {
            var body = $"""
                        <div dir="rtl" style="font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f0fdfa; padding: 40px 20px; text-align: center;">
                            <div style="max-width: 500px; margin: 0 auto; background-color: #ffffff; padding: 30px; border-radius: 16px; box-shadow: 0 4px 6px rgba(0,0,0,0.05); border-top: 5px solid #0d9488;">
                                <h2 style="color: #0d9488; margin-top: 0; font-size: 24px;">💊 حان موعد الدواء!</h2>
                                <p style="font-size: 16px; color: #334155; line-height: 1.6;">نتمنى لك دوام الصحة والعافية، نذكرك بأنه قد حان موعد تناول جرعتك الآن.</p>
                                
                                <div style="background-color: #f8fafc; border-radius: 12px; padding: 20px; margin: 25px 0; border: 1px solid #e2e8f0; text-align: right;">
                                    <p style="margin: 0 0 10px 0; font-size: 18px; color: #0f172a;">اسم الدواء: <strong style="color: #0d9488;">{reminder.MedicineName}</strong></p>
                                    {(reminder.Dosage != null ? $"<p style=\"margin: 0 0 10px 0; color: #475569;\">الجرعة: <strong>{reminder.Dosage}</strong></p>" : "")}
                                    {(reminder.Notes != null ? $"<p style=\"margin: 0; color: #64748b; font-size: 14px;\">ملاحظات: {reminder.Notes}</p>" : "")}
                                </div>
                                
                                <a href="https://pharmalink.vercel.app/patient/reminders" style="display: inline-block; background-color: #0d9488; color: #ffffff; text-decoration: none; padding: 12px 24px; border-radius: 8px; font-weight: bold; margin-top: 10px;">عرض التذكيرات في التطبيق</a>
                                
                                <div style="margin-top: 30px; border-top: 1px solid #e2e8f0; padding-top: 15px;">
                                    <p style="color: #94a3b8; font-size: 12px; margin: 0;">فارما لينك - رعاية صحتك أولويتنا 🌿</p>
                                </div>
                            </div>
                        </div>
                        """;

            await emailService.SendEmailAsync(email, $"💊 تذكير: {reminder.MedicineName}", body);
            context.MedicineReminderLogs.Add(new MedicineReminderLog
            {
                ReminderId = reminder.Id, SentAt = now, Channel = ReminderChannel.Email, IsSuccess = true
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send reminder email for reminder {Id}", reminder.Id);
            context.MedicineReminderLogs.Add(new MedicineReminderLog
            {
                ReminderId = reminder.Id, SentAt = now, Channel = ReminderChannel.Email,
                IsSuccess = false, ErrorMessage = ex.Message
            });
        }

        await context.SaveChangesAsync();
    }

    private async Task TrySendWhatsApp(MedicineReminder reminder, string phone, DateTime now)
    {
        try
        {
            var message = $"💊 تذكير فارما لينك\n\nحان موعد تناول دوائك: *{reminder.MedicineName}*"
                          + (reminder.Dosage != null ? $"\nالجرعة: {reminder.Dosage}" : "")
                          + (reminder.Notes != null ? $"\nملاحظات: {reminder.Notes}" : "");

            await whatsAppService.SendMessageAsync(phone, message);

            context.MedicineReminderLogs.Add(new MedicineReminderLog
            {
                ReminderId = reminder.Id, SentAt = now, Channel = ReminderChannel.WhatsApp, IsSuccess = true
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send WhatsApp reminder for reminder {Id}", reminder.Id);
            context.MedicineReminderLogs.Add(new MedicineReminderLog
            {
                ReminderId = reminder.Id, SentAt = now, Channel = ReminderChannel.WhatsApp,
                IsSuccess = false, ErrorMessage = ex.Message
            });
        }

        await context.SaveChangesAsync();
    }

    private static ReminderResponseDto MapToDto(MedicineReminder r)
    {
        var times = new List<string>();
        try
        {
            times = JsonSerializer.Deserialize<List<string>>(r.ReminderTimesJson) ?? new();
        }
        catch
        {
        }

        return new ReminderResponseDto
        {
            Id = r.Id,
            MedicineName = r.MedicineName,
            Dosage = r.Dosage,
            Notes = r.Notes,
            ReminderTimes = times,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            NotifyByEmail = r.NotifyByEmail,
            NotifyByWhatsApp = r.NotifyByWhatsApp,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt
        };
    }
}