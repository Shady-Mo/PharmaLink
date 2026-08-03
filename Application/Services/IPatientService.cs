using Application.DTOs;
using Application.DTOs.Patient;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services;

public interface IPatientService
{
    Task<Result<PatientProfileDto>> GetProfileAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<Result<PatientProfileDto>> UpdateProfileAsync(Guid patientId, UpdatePatientProfileDto updateDto, CancellationToken cancellationToken = default);
    Task<Result> UploadProfilePictureAsync(Guid patientId, UploadProfilePictureDto dto, string baseUrl, CancellationToken cancellationToken = default);
    Task<Result<string>> GetProfilePictureUrlAsync(Guid patientId, CancellationToken cancellationToken = default);
}