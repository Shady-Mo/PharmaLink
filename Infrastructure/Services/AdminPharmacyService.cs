namespace Infrastructure.Services.Pharmacy
{
    public class AdminPharmacyService(AppDbContext context, IMapper mapper, IWebHostEnvironment env) : IAdminPharmacyService
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

            var projectedQuery = query
                .ProjectToType<AdminPharmacySummaryDTO>()
                .OrderBy(p => p.LegalName);

            var paginated = await projectedQuery.ToPaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
            return Result.Success(paginated);
        }

        public async Task<Result<AdminPharmacyDetailDTO>> GetPharmacyByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var pharmacy = await context.Pharmacies
                .AsNoTracking()
                .ProjectToType<AdminPharmacyDetailDTO>()
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
                var webRoot = env.WebRootPath ?? System.IO.Path.Combine(env.ContentRootPath, "wwwroot");
                var uploadsFolder = System.IO.Path.Combine(webRoot, "uploads", "logos");
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
                var webRoot = env.WebRootPath ?? System.IO.Path.Combine(env.ContentRootPath, "wwwroot");
                var uploadsFolder = System.IO.Path.Combine(webRoot, "uploads", "logos");
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
            var pharmacy = await context.Pharmacies
                .Include(p => p.Admins)
                .FirstOrDefaultAsync(p => p.PharmacyId == id, cancellationToken);
            if (pharmacy is null)
                return Result.Failure(PharmacyErrors.PharmacyNotFound);

            pharmacy.VerificationStatus = VerificationStatus.Deleted;
            foreach (var adminUser in pharmacy.Admins)
            {
                adminUser.IsSuperAdmin = false;
            }

            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result> ChangePharmacyStatusAsync(
            Guid id,
            VerificationStatus status,
            CancellationToken cancellationToken = default)
        {
            var pharmacy = await context.Pharmacies
                .Include(p => p.Admins)
                .FirstOrDefaultAsync(p => p.PharmacyId == id, cancellationToken);
            if (pharmacy is null)
                return Result.Failure(PharmacyErrors.PharmacyNotFound);

            pharmacy.VerificationStatus = status;
            if (status == VerificationStatus.Deleted || status == VerificationStatus.Rejected)
            {
                foreach (var adminUser in pharmacy.Admins)
                {
                    adminUser.IsSuperAdmin = false;
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result> AssignOwnerAsync(
            Guid pharmacyId,
            Guid ownerId,
            CancellationToken cancellationToken = default)
        {
            var pharmacy = await context.Pharmacies.FirstOrDefaultAsync(p => p.PharmacyId == pharmacyId, cancellationToken);
            if (pharmacy is null)
                return Result.Failure(PharmacyErrors.PharmacyNotFound);

            if (pharmacy.VerificationStatus == VerificationStatus.Deleted || pharmacy.VerificationStatus == VerificationStatus.Rejected)
                return Result.Failure(PharmacyErrors.PharmacyNotEligible);

            var admin = await context.PharmacyAdmins.FirstOrDefaultAsync(a => a.Id == ownerId, cancellationToken);
            if (admin is null)
                return Result.Failure(PharmacyOwnerErrors.PharmacyOwnerNotFound);

            if (admin.Status != UserStatus.Active)
                return Result.Failure(PharmacyOwnerErrors.OwnerNotActive);

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
