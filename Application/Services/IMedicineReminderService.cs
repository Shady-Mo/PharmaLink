using Application.DTOs.MedicineReminder;

namespace Application.Services;

public interface IMedicineReminderService
{
    Task<Result<ReminderResponseDto>> CreateAsync(Guid patientId, CreateReminderRequest request);
    Task<Result<ReminderResponseDto>> UpdateAsync(Guid id, Guid patientId, CreateReminderRequest request);
    Task<Result> DeleteAsync(Guid id, Guid patientId);
    Task<Result> ToggleAsync(Guid id, Guid patientId);
    Task<Result<List<ReminderResponseDto>>> GetPatientRemindersAsync(Guid patientId);
    Task ProcessDueRemindersAsync(); // called by Hangfire
}
