namespace Infrastructure.Services;

public class PatientService(
    AppDbContext context,
    ILogger<PatientService> logger) : IPatientService
{
    public async Task<Result<PatientProfileDto>> GetProfileAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // جلب المريض مع العناوين المسجلة باسمه
        var patient = await context.Patients
            .Include(p => p.Addresses)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);

        if (patient is null)
        {
            logger.LogWarning("Profile fetch failed: Patient with ID {PatientId} was not found.", patientId);
            return Result.Failure<PatientProfileDto>(PatientErrors.PatientNotFound);
        }

        // تحويل البيانات إلى الـ DTO المطلوب مع استخراج إحداثيات الـ Spatial Point بدقة
        var profileDto = new PatientProfileDto
        {
            PatientId = patient.Id,
            FullName = patient.FullName,
            Email = patient.Email ?? string.Empty,
            PhoneNumber = patient.PhoneNumber ?? string.Empty,
            Status = patient.Status.ToString(),
            CreatedAt = patient.CreatedAt,
            Addresses = patient.Addresses.Select(a => new PatientAddressDto
            {
                AddressId = a.AddressId,
                //Label = a.Label,
                AddressLine = a.AddressLine,
                City = a.City,
                Governorate = a.Governorate,
                IsDefault = a.IsDefault,
                Latitude = a.GeoLocation?.Y, // خط العرض من نقطة الـ Spatial
                Longitude = a.GeoLocation?.X // خط الطول من نقطة الـ Spatial
            }).ToList()
        };

        return Result.Success(profileDto);
    }


    public async Task<Result<PatientProfileDto>> UpdateProfileAsync(Guid patientId, UpdatePatientProfileDto updateDto, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 1. جلب المريض من قاعدة البيانات مع العناوين المرتبطة به لغرض التعديل (دون استخدام AsNoTracking)
        var patient = await context.Patients
            //.Include(p => p.Addresses)
            .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);

        if (patient is null)
        {
            logger.LogWarning("Profile update failed: Patient with ID {PatientId} was not found.", patientId);
            return Result.Failure<PatientProfileDto>(PatientErrors.PatientNotFound);
        }

        patient.FullName = updateDto.FullName;
        patient.PhoneNumber = updateDto.PhoneNumber;


        
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Patient profile updated successfully for ID {PatientId}.", patientId);

        var updatedProfileDto = MapToProfileDto(patient);
        return Result.Success(updatedProfileDto);
    }

    private static PatientProfileDto MapToProfileDto(Domain.Entities.Patient patient)
    {
        return new PatientProfileDto
        {
            PatientId = patient.Id,
            FullName = patient.FullName,
            Email = patient.Email ?? string.Empty,
            PhoneNumber = patient.PhoneNumber ?? string.Empty,
            Status = patient.Status.ToString(),
            CreatedAt = patient.CreatedAt,

            //Addresses = patient.Addresses.Select(a => new PatientAddressDto
            //{
            //    AddressId = a.AddressId,
            //    AddressLine = a.AddressLine,
            //    City = a.City,
            //    Governorate = a.Governorate,
            //    IsDefault = a.IsDefault,
            //    Latitude = a.GeoLocation?.Y,
            //    Longitude = a.GeoLocation?.X
            //}).ToList()
        };
    }


}