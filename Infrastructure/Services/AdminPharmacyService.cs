using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common;
using Application.DTOs;
using Application.DTOs.Pharmacy.Request;
using Application.DTOs.Pharmacy.Responses;
using Application.Errors;
using Application.Services.Pharmacy;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Pharmacy
{
    public class AdminPharmacyService(AppDbContext context) : IAdminPharmacyService
    {
        public async Task<Result<PaginatedList<AdminPharmacySummaryDTO>>> GetAllPharmaciesAsync(
            GetAdminPharmaciesRequest request,
            CancellationToken cancellationToken = default)
        {
            var query = context.Pharmacies.AsNoTracking();

            if (request.Status.HasValue)
            {
                query = query.Where(p => p.VerificationStatus == request.Status.Value);
            }
            else
            {
                query = query.Where(p => p.VerificationStatus != VerificationStatus.Deleted);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchTerm = request.Search.Trim().ToLower();
                query = query.Where(p => p.LegalName.ToLower().Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(request.City))
            {
                var cityTerm = request.City.Trim().ToLower();
                query = query.Where(p => p.Branches.Any(b => b.City.ToLower().Contains(cityTerm)));
            }

            var projectedQuery = query.Select(p => new AdminPharmacySummaryDTO
            {
                PharmacyId = p.PharmacyId,
                LegalName = p.LegalName,
                LicenseNumber = p.LicenseNumber,
                LogoUrl = p.LogoUrl,
                VerificationStatus = p.VerificationStatus,
                BranchesCount = p.Branches.Count,
                DrugsCount = p.Branches.SelectMany(b => b.Inventories).Select(i => i.DrugId).Distinct().Count(),
                Owner = p.Admins
                    .Where(a => a.IsSuperAdmin == true)
                    .Select(a => new PharmacyOwnerDTO
                    {
                        Id = a.Id,
                        FullName = a.FullName,
                        Email = a.Email ?? string.Empty,
                        PhoneNumber = a.PhoneNumber ?? string.Empty
                    })
                    .FirstOrDefault(),
                Branches = p.Branches.Select(b => new AdminPharmacyBranchDTO
                {
                    BranchId = b.BranchId,
                    BranchName = b.BranchName,
                    City = b.City,
                    Governorate = b.Governorate,
                    PhoneNumber = b.PhoneNumber,
                    WorkingHours = b.WorkingHours,
                    Latitude = b.GeoLocation != null ? b.GeoLocation.Y : 0,
                    Longitude = b.GeoLocation != null ? b.GeoLocation.X : 0,
                    ServiceRadiusKm = b.ServiceRadiusKm,
                    SupportsDelivery = b.SupportsDelivery,
                    SupportsPickup = b.SupportsPickup
                }).ToList()
            }).OrderBy(p => p.LegalName);

            var paginated = await projectedQuery.ToPaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
            return Result.Success(paginated);
        }

        public async Task<Result<AdminPharmacyDetailDTO>> GetPharmacyByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var pharmacy = await context.Pharmacies
                .AsNoTracking()
                .Select(p => new AdminPharmacyDetailDTO
                {
                    PharmacyId = p.PharmacyId,
                    LegalName = p.LegalName,
                    LicenseNumber = p.LicenseNumber,
                    LogoUrl = p.LogoUrl,
                    VerificationStatus = p.VerificationStatus,
                    BranchesCount = p.Branches.Count,
                    DrugsCount = p.Branches.SelectMany(b => b.Inventories).Select(i => i.DrugId).Distinct().Count(),
                    Owner = p.Admins
                        .Where(a => a.IsSuperAdmin == true)
                        .Select(a => new PharmacyOwnerDTO
                        {
                            Id = a.Id,
                            FullName = a.FullName,
                            Email = a.Email ?? string.Empty,
                            PhoneNumber = a.PhoneNumber ?? string.Empty
                        })
                        .FirstOrDefault(),
                    Branches = p.Branches.Select(b => new AdminPharmacyBranchDTO
                    {
                        BranchId = b.BranchId,
                        BranchName = b.BranchName,
                        City = b.City,
                        Governorate = b.Governorate,
                        PhoneNumber = b.PhoneNumber,
                        WorkingHours = b.WorkingHours,
                        Latitude = b.GeoLocation != null ? b.GeoLocation.Y : 0,
                        Longitude = b.GeoLocation != null ? b.GeoLocation.X : 0,
                        ServiceRadiusKm = b.ServiceRadiusKm,
                        SupportsDelivery = b.SupportsDelivery,
                        SupportsPickup = b.SupportsPickup
                    }).ToList()
                })
                .FirstOrDefaultAsync(p => p.PharmacyId == id, cancellationToken);

            if (pharmacy is null)
                return Result.Failure<AdminPharmacyDetailDTO>(PharmacyErrors.PharmacyNotFound);

            return Result.Success(pharmacy);
        }

        public async Task<Result<Guid>> CreatePharmacyAsync(
            AdminCreatePharmacyDTO dto,
            CancellationToken cancellationToken = default)
        {
            var isLicenseUnique = !await context.Pharmacies
                .AnyAsync(p => p.LicenseNumber == dto.LicenseNumber, cancellationToken);

            if (!isLicenseUnique)
                return Result.Failure<Guid>(PharmacyErrors.LicenseNumberNotUnique);

            string logoUrl = dto.LogoUrl ?? string.Empty;

            if (dto.LogoFile != null && dto.LogoFile.Length > 0)
            {
                var uploadsFolder = System.IO.Path.Combine("wwwroot", "uploads", "logos");
                if (!System.IO.Directory.Exists(uploadsFolder))
                {
                    System.IO.Directory.CreateDirectory(uploadsFolder);
                }

                var fileExtension = System.IO.Path.GetExtension(dto.LogoFile.FileName).ToLowerInvariant();
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var absolutePath = System.IO.Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new System.IO.FileStream(absolutePath, System.IO.FileMode.Create))
                {
                    await dto.LogoFile.CopyToAsync(stream, cancellationToken);
                }

                logoUrl = $"/uploads/logos/{uniqueFileName}";
            }

            var pharmacy = new Domain.Entities.Pharmacy
            {
                PharmacyId = Guid.NewGuid(),
                LegalName = dto.LegalName,
                LicenseNumber = dto.LicenseNumber,
                LogoUrl = logoUrl,
                VerificationStatus = dto.VerificationStatus
            };

            await context.Pharmacies.AddAsync(pharmacy, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(pharmacy.PharmacyId);
        }

        public async Task<Result> UpdatePharmacyAsync(
            Guid id,
            AdminUpdatePharmacyDTO dto,
            CancellationToken cancellationToken = default)
        {
            var pharmacy = await context.Pharmacies.FirstOrDefaultAsync(p => p.PharmacyId == id, cancellationToken);
            if (pharmacy is null)
                return Result.Failure(PharmacyErrors.PharmacyNotFound);

            var isLicenseUnique = !await context.Pharmacies
                .AnyAsync(p => p.LicenseNumber == dto.LicenseNumber && p.PharmacyId != id, cancellationToken);

            if (!isLicenseUnique)
                return Result.Failure(PharmacyErrors.LicenseNumberNotUnique);

            pharmacy.LegalName = dto.LegalName;
            pharmacy.LicenseNumber = dto.LicenseNumber;
            pharmacy.VerificationStatus = dto.VerificationStatus;

            if (dto.LogoFile != null && dto.LogoFile.Length > 0)
            {
                var uploadsFolder = System.IO.Path.Combine("wwwroot", "uploads", "logos");
                if (!System.IO.Directory.Exists(uploadsFolder))
                {
                    System.IO.Directory.CreateDirectory(uploadsFolder);
                }

                var fileExtension = System.IO.Path.GetExtension(dto.LogoFile.FileName).ToLowerInvariant();
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var absolutePath = System.IO.Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new System.IO.FileStream(absolutePath, System.IO.FileMode.Create))
                {
                    await dto.LogoFile.CopyToAsync(stream, cancellationToken);
                }

                pharmacy.LogoUrl = $"/uploads/logos/{uniqueFileName}";
            }
            else if (dto.LogoUrl != null)
            {
                pharmacy.LogoUrl = dto.LogoUrl;
            }

            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result> SoftDeletePharmacyAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var pharmacy = await context.Pharmacies.FirstOrDefaultAsync(p => p.PharmacyId == id, cancellationToken);
            if (pharmacy is null)
                return Result.Failure(PharmacyErrors.PharmacyNotFound);

            pharmacy.VerificationStatus = VerificationStatus.Deleted;
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result> ChangePharmacyStatusAsync(
            Guid id,
            VerificationStatus status,
            CancellationToken cancellationToken = default)
        {
            var pharmacy = await context.Pharmacies.FirstOrDefaultAsync(p => p.PharmacyId == id, cancellationToken);
            if (pharmacy is null)
                return Result.Failure(PharmacyErrors.PharmacyNotFound);

            pharmacy.VerificationStatus = status;
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result> AssignOwnerAsync(
            Guid pharmacyId,
            Guid ownerId,
            CancellationToken cancellationToken = default)
        {
            var pharmacyExists = await context.Pharmacies.AnyAsync(p => p.PharmacyId == pharmacyId, cancellationToken);
            if (!pharmacyExists)
                return Result.Failure(PharmacyErrors.PharmacyNotFound);

            var admin = await context.PharmacyAdmins.FirstOrDefaultAsync(a => a.Id == ownerId, cancellationToken);
            if (admin is null)
                return Result.Failure(new Error("PharmacyAdminNotFound", "The specified user is not registered as a Pharmacy Admin.", 404));

            // Clear any existing super admins for this pharmacy
            var existingSuperAdmins = await context.PharmacyAdmins
                .Where(a => a.PharmacyId == pharmacyId && a.IsSuperAdmin == true)
                .ToListAsync(cancellationToken);

            foreach (var existingAdmin in existingSuperAdmins)
            {
                existingAdmin.IsSuperAdmin = false;
            }

            // Assign new owner
            admin.PharmacyId = pharmacyId;
            admin.IsSuperAdmin = true;

            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
