using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;

namespace Application.Services;

public interface IPatientService
{
    Task<Result<PatientProfileDto>> GetProfileAsync(Guid patientId, CancellationToken cancellationToken = default);
}