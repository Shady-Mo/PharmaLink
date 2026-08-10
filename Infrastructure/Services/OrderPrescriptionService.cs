namespace Infrastructure.Services;

using Application.DTOs.Prescriptions;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Application.Common;
using Infrastructure.Persistence;
using System.IO;

public class OrderPrescriptionService(
    AppDbContext context,
    IWebHostEnvironment env) : IOrderPrescriptionService
{
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };
    private readonly string[] _allowedMimeTypes = { "image/jpeg", "image/png", "application/pdf" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    public async Task<Result<PrescriptionResponseDto>> UploadPrescriptionAsync(Guid userId, UploadPrescriptionRequest request, string baseUrl,
        CancellationToken cancellationToken)
    {
        var file = request.File;
        if (file == null || file.Length == 0)
        {
            return Result.Failure<PrescriptionResponseDto>(new Error("OrderPrescription.EmptyFile", "Empty file submitted.", StatusCodes.Status400BadRequest));
        }

        if (file.Length > MaxFileSize)
        {
            return Result.Failure<PrescriptionResponseDto>(new Error("OrderPrescription.FileSizeExceeded", "File size exceeds the 5MB limit.", StatusCodes.Status400BadRequest));
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension) || !_allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            return Result.Failure<PrescriptionResponseDto>(new Error("OrderPrescription.InvalidFileType", "Invalid file type. Only JPG, PNG, and PDF are allowed.", StatusCodes.Status400BadRequest));
        }

        // Validate Magic Bytes
        using var stream = file.OpenReadStream();
        var buffer = new byte[4];
        await stream.ReadAsync(buffer, 0, 4, cancellationToken);
        if (!IsValidMagicBytes(buffer, extension))
        {
            return Result.Failure<PrescriptionResponseDto>(new Error("OrderPrescription.InvalidFileContent", "File content does not match its extension.", StatusCodes.Status400BadRequest));
        }

        // Reset stream position to start
        stream.Position = 0;

        var uploadsFolder = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "prescriptions");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var absolutePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var fileStream = new FileStream(absolutePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream, cancellationToken);
        }

        var prescriptionId = Guid.NewGuid();
        var prescription = new Prescription
        {
            Id = prescriptionId,
            PatientId = userId,
            OrderId = null,
            Status = PrescriptionStatus.Pending,
            FileName = file.FileName,
            FileUrl = $"{baseUrl}/prescriptions/{uniqueFileName}",
            StoragePath = absolutePath,
            ContentType = file.ContentType,
            FileSize = file.Length,
            UploadedAt = DateTime.UtcNow
        };

        context.Prescriptions.Add(prescription);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success<PrescriptionResponseDto>(new PrescriptionResponseDto
        {
            Id = prescription.Id,
            FileUrl = prescription.FileUrl,
            Status = prescription.Status,
            UploadedAt = prescription.UploadedAt,
            RejectionReason = prescription.RejectionReason
        });
    }

    public async Task<Result<PrescriptionResponseDto>> GetPrescriptionDetailsAsync(Guid prescriptionId, Guid userId, string userRole, CancellationToken cancellationToken)
    {
        var prescription = await context.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == prescriptionId, cancellationToken);

        if (prescription == null)
        {
            return Result.Failure<PrescriptionResponseDto>(new Error("OrderPrescription.NotFound", "Prescription not found.", StatusCodes.Status404NotFound));
        }

        // Security check
        if (prescription.PatientId != userId && userRole == AppRoles.Patient)
        {
            return Result.Failure<PrescriptionResponseDto>(new Error("OrderPrescription.Unauthorized", "You are not authorized to view this prescription.", StatusCodes.Status403Forbidden));
        }

        return Result.Success<PrescriptionResponseDto>(new PrescriptionResponseDto
        {
            Id = prescription.Id,
            FileUrl = prescription.FileUrl,
            Status = prescription.Status,
            UploadedAt = prescription.UploadedAt,
            RejectionReason = prescription.RejectionReason
        });
    }

    public async Task<Result<(Stream Stream, string ContentType, string FileName)>> GetPrescriptionFileAsync(
        Guid prescriptionId,
        Guid userId,
        string userRole,
        CancellationToken cancellationToken)
    {
        var prescription = await context.Prescriptions
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == prescriptionId, cancellationToken);

        if (prescription == null)
        {
            return Result.Failure<(Stream Stream, string ContentType, string FileName)>(new Error("OrderPrescription.NotFound", "Prescription not found.", StatusCodes.Status404NotFound));
        }

        // Security check: Only the patient who uploaded it, or pharmacist/admin/review team can view it
        if (prescription.PatientId != userId && userRole == AppRoles.Patient)
        {
            return Result.Failure<(Stream Stream, string ContentType, string FileName)>(new Error("OrderPrescription.Unauthorized", "You are not authorized to view this prescription.", StatusCodes.Status403Forbidden));
        }

        if (!File.Exists(prescription.StoragePath))
        {
            return Result.Failure<(Stream Stream, string ContentType, string FileName)>(new Error("OrderPrescription.FileNotFound", "File not found on server.", StatusCodes.Status404NotFound));
        }

        var fileStream = new FileStream(prescription.StoragePath, FileMode.Open, FileAccess.Read);

        return Result.Success<(Stream Stream, string ContentType, string FileName)>(((Stream)fileStream, prescription.ContentType, prescription.FileName));
    }

    private bool IsValidMagicBytes(byte[] buffer, string extension)
    {
        if (extension == ".pdf" && buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46)
        {
            return true;
        }

        if ((extension == ".jpg" || extension == ".jpeg") && buffer[0] == 0xFF && buffer[1] == 0xD8 &&
            buffer[2] == 0xFF)
        {
            return true;
        }

        if (extension == ".png" && buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47)
        {
            return true;
        }

        return false;
    }
}