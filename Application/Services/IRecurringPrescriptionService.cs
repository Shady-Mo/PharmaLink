using Application.DTOs.RecurringPrescription;

namespace Application.Services;

public interface IRecurringPrescriptionService
{
    Task<Result<RecurringResponseDto>> CreateAsync(Guid patientId, CreateRecurringRequest request);
    Task<Result<RecurringResponseDto>> UpdateAsync(Guid id, Guid patientId, CreateRecurringRequest request);
    Task<Result> PauseAsync(Guid id, Guid patientId);
    Task<Result> ResumeAsync(Guid id, Guid patientId);
    Task<Result> DeleteAsync(Guid id, Guid patientId);
    Task<Result<List<RecurringResponseDto>>> GetPatientRecurringAsync(Guid patientId);
    Task<Result> ConfirmRunAsync(Guid runId, string token);
    Task<Result> SkipRunAsync(Guid runId, Guid patientId);
    Task ProcessDueRecurringAsync(); // called by Hangfire daily
    Task AutoConfirmExpiredRunsAsync(); // auto-confirm if patient didn't respond in 24h
}
