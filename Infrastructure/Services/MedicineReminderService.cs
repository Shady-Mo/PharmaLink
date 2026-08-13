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
        var currentTime = TimeOnly.FromDateTime(localNow);
        var windowStart = currentTime.AddMinutes(-1);
        var windowEnd = currentTime.AddMinutes(1);

        logger.LogInformation("--- START ProcessDueRemindersAsync ---");
        logger.LogInformation("UTC Now: {UtcNow}", utcNow);
        logger.LogInformation("Egypt Local Now: {LocalNow}", localNow);
        logger.LogInformation("Current Egypt Time: {CurrentTime}", currentTime);
        logger.LogInformation("Window: {WindowStart} - {WindowEnd}", windowStart, windowEnd);

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
            logger.LogInformation("Checking Reminder {Id} ({Name}) - Times: {TimesJson}", reminder.Id, reminder.MedicineName, reminder.ReminderTimesJson);

            foreach (var timeStr in times)
            {
                if (!TimeOnly.TryParse(timeStr, out var reminderTime))
                {
                    logger.LogWarning("Reminder {Id}: Could not parse time '{TimeStr}'", reminder.Id, timeStr);
                    continue;
                }

                if (reminderTime < windowStart || reminderTime > windowEnd)
                {
                    continue;
                }

                logger.LogInformation("Reminder Time: {TimeStr}", timeStr);
                logger.LogInformation("MATCH FOUND");

                // Check if already sent in this window (within last 2 minutes)
                var alreadySent = await context.MedicineReminderLogs
                    .AnyAsync(l => l.ReminderId == reminder.Id
                                   && l.SentAt >= utcNow.AddMinutes(-2)
                                   && l.IsSuccess);
                if (alreadySent)
                {
                    logger.LogInformation("Reminder {Id}: Already sent successfully within the last 2 minutes. Skipping.", reminder.Id);
                    continue;
                }

                logger.LogInformation("Reminder {Id}: Ready to send! Patient: {PatientId}", reminder.Id, reminder.PatientId);
                var patient = reminder.Patient;

                if (reminder.NotifyByEmail && !string.IsNullOrEmpty(patient.Email))
                {
                    logger.LogInformation("Reminder {Id}: Sending Email to {Email}", reminder.Id, patient.Email);
                    await TrySendEmail(reminder, patient.Email, utcNow);
                }

                if (reminder.NotifyByWhatsApp && !string.IsNullOrEmpty(patient.PhoneNumber))
                {
                    logger.LogInformation("Reminder {Id}: Sending WhatsApp to {Phone}", reminder.Id, patient.PhoneNumber);
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
                        <div dir="rtl" style="font-family: Arial; padding: 20px;">
                            <h2>💊 تذكير بموعد الدواء</h2>
                            <p>حان موعد تناول دوائك: <strong>{reminder.MedicineName}</strong></p>
                            {(reminder.Dosage != null ? $"<p>الجرعة: <strong>{reminder.Dosage}</strong></p>" : "")}
                            {(reminder.Notes != null ? $"<p>ملاحظات: {reminder.Notes}</p>" : "")}
                            <p style="color: #666; font-size: 12px;">فارما لينك - رعاية صحتك أولويتنا 🌿</p>
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