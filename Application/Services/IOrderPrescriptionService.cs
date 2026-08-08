using Application.DTOs.Prescriptions;

namespace Application.Services;

public interface IOrderPrescriptionService
{
    Task<Result<PrescriptionResponseDto>> UploadPrescriptionAsync(Guid userId, UploadPrescriptionRequest request,
        string baseUrl, CancellationToken cancellationToken);

    Task<Result<PrescriptionResponseDto>> GetPrescriptionDetailsAsync(Guid prescriptionId, Guid userId, string userRole,
        CancellationToken cancellationToken);

    Task<Result<(Stream Stream, string ContentType, string FileName)>> GetPrescriptionFileAsync(
        Guid prescriptionId,
        Guid userId,
        string userRole,
        CancellationToken cancellationToken);
}