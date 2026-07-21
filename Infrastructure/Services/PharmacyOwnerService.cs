
using Application.DTOs.PharmacyOwner.Request;
using Application.DTOs.PharmacyOwner.Responses;

namespace Infrastructure.Services.PharmacyOwner
{
    public class PharmacyOwnerService(
        AppDbContext context,
        UserManager<AppUser> userManager,
        IMapper mapper,
        ILogger<PharmacyOwnerService> logger) : IPharmacyOwnerService
    {
        public async Task<Result<PharmacyOwnerResponseDTO>> CreatePharmacyOwnerAsync(
            CreatePharmacyOwnerDTO dto,
            CancellationToken cancellationToken = default)
        {
            var existingByEmail = await userManager.FindByEmailAsync(dto.Email);
            if (existingByEmail is not null)
            {
                logger.LogWarning("Admin tried to create pharmacy owner with existing email: {Email}", dto.Email);
                return Result.Failure<PharmacyOwnerResponseDTO>(PharmacyOwnerErrors.EmailAlreadyExists);
            }

            var existingByPhone = await context.AppUsers
                .AnyAsync(u => u.PhoneNumber == dto.PhoneNumber, cancellationToken);
            if (existingByPhone)
            {
                logger.LogWarning("Admin tried to create pharmacy owner with existing phone number: {Phone}", dto.PhoneNumber);
                return Result.Failure<PharmacyOwnerResponseDTO>(PharmacyOwnerErrors.PhoneAlreadyExists);
            }

            if (dto.PharmacyId.HasValue)
            {
                var pharmacyExists = await context.Pharmacies.AnyAsync(p => p.PharmacyId == dto.PharmacyId.Value, cancellationToken);
                if (!pharmacyExists)
                    return Result.Failure<PharmacyOwnerResponseDTO>(PharmacyErrors.PharmacyNotFound);
            }

            var admin = new PharmacyAdmin
            {
                Id = Guid.NewGuid(),
                FullName = dto.FullName,
                Email = dto.Email.ToLowerInvariant(),
                UserName = dto.Email.ToLowerInvariant(),
                PhoneNumber = dto.PhoneNumber,
                PharmacyId = dto.PharmacyId,
                IsSuperAdmin = true, // By default, registered as owner/super admin
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };

            var createResult = await userManager.CreateAsync(admin, dto.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                logger.LogError("Identity failed to create pharmacy owner. Errors: {Errors}", errors);
                return Result.Failure<PharmacyOwnerResponseDTO>(new Error("PharmacyOwner.RegistrationFailed", errors, StatusCodes.Status500InternalServerError));
            }

            var roleResult = await userManager.AddToRoleAsync(admin, AppRoles.PharmacyAdmin);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                logger.LogError("Identity failed to add pharmacy owner to role. Errors: {Errors}", errors);
                return Result.Failure<PharmacyOwnerResponseDTO>(new Error("PharmacyOwner.RoleAssignmentFailed", errors, StatusCodes.Status500InternalServerError));
            }

            // Clear other super admins/owners for this pharmacy
            if (dto.PharmacyId.HasValue)
            {
                var otherSuperAdmins = await context.PharmacyAdmins
                    .Where(a => a.PharmacyId == dto.PharmacyId.Value && a.Id != admin.Id && a.IsSuperAdmin == true)
                    .ToListAsync(cancellationToken);

                foreach (var other in otherSuperAdmins)
                {
                    other.IsSuperAdmin = false;
                }
                await context.SaveChangesAsync(cancellationToken);
            }

            // Load complete entity with relation for mapping
            var createdAdmin = await context.PharmacyAdmins
                .Include(a => a.Pharmacy)
                .FirstOrDefaultAsync(a => a.Id == admin.Id, cancellationToken);

            return Result.Success(mapper.Map<PharmacyOwnerResponseDTO>(createdAdmin));
        }

        public async Task<Result<PharmacyOwnerResponseDTO>> GetPharmacyOwnerByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var admin = await context.PharmacyAdmins
                .AsNoTracking()
                .Include(a => a.Pharmacy)
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

            if (admin is null)
                return Result.Failure<PharmacyOwnerResponseDTO>(PharmacyOwnerErrors.PharmacyOwnerNotFound);

            return Result.Success(mapper.Map<PharmacyOwnerResponseDTO>(admin));
        }

        public async Task<Result<PaginatedList<PharmacyOwnerResponseDTO>>> GetAllPharmacyOwnersAsync(
            GetPharmacyOwnersRequest request,
            CancellationToken cancellationToken = default)
        {
            var query = context.PharmacyAdmins
                .AsNoTracking()
                .Include(a => a.Pharmacy)
                .Where(a => a.IsSuperAdmin == true) // Filter only owners (super admins)
                .AsQueryable();

            if (request.Status.HasValue)
            {
                query = query.Where(a => a.Status == request.Status.Value);
            }
            else
            {
                query = query.Where(a => a.Status != UserStatus.Inactive);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = request.Search.Trim().ToLower();
                query = query.Where(a => a.FullName.ToLower().Contains(term) || a.Email.ToLower().Contains(term));
            }

            if (request.PharmacyId.HasValue)
            {
                query = query.Where(a => a.PharmacyId == request.PharmacyId.Value);
            }

            var projected = query
                .ProjectToType<PharmacyOwnerResponseDTO>()
                .OrderBy(a => a.FullName);

            var paginated = await projected.ToPaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
            return Result.Success(paginated);
        }

        public async Task<Result> UpdatePharmacyOwnerAsync(
            Guid id,
            UpdatePharmacyOwnerDTO dto,
            CancellationToken cancellationToken = default)
        {
            var admin = await context.PharmacyAdmins.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (admin is null)
                return Result.Failure(PharmacyOwnerErrors.PharmacyOwnerNotFound);

            var isEmailUnique = !await context.AppUsers
                .AnyAsync(u => u.Email == dto.Email && u.Id != id, cancellationToken);
            if (!isEmailUnique)
                return Result.Failure(PharmacyOwnerErrors.EmailAlreadyExists);

            var isPhoneUnique = !await context.AppUsers
                .AnyAsync(u => u.PhoneNumber == dto.PhoneNumber && u.Id != id, cancellationToken);
            if (!isPhoneUnique)
                return Result.Failure(PharmacyOwnerErrors.PhoneAlreadyExists);

            if (dto.PharmacyId.HasValue)
            {
                var pharmacyExists = await context.Pharmacies.AnyAsync(p => p.PharmacyId == dto.PharmacyId.Value, cancellationToken);
                if (!pharmacyExists)
                    return Result.Failure(PharmacyErrors.PharmacyNotFound);
            }

            admin.FullName = dto.FullName;
            admin.Email = dto.Email.ToLowerInvariant();
            admin.UserName = dto.Email.ToLowerInvariant();
            admin.PhoneNumber = dto.PhoneNumber;
            admin.PharmacyId = dto.PharmacyId;
            admin.IsSuperAdmin = true; // Ensure they remain owner
            admin.Status = dto.Status;

            // Clear other super admins/owners for this pharmacy
            if (dto.PharmacyId.HasValue)
            {
                var otherSuperAdmins = await context.PharmacyAdmins
                    .Where(a => a.PharmacyId == dto.PharmacyId.Value && a.Id != id && a.IsSuperAdmin == true)
                    .ToListAsync(cancellationToken);

                foreach (var other in otherSuperAdmins)
                {
                    other.IsSuperAdmin = false;
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result> SoftDeletePharmacyOwnerAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var admin = await context.PharmacyAdmins.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (admin is null)
                return Result.Failure(PharmacyOwnerErrors.PharmacyOwnerNotFound);

            admin.Status = UserStatus.Inactive;
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result> AssignOwnerAsync(
            Guid userId,
            Guid pharmacyId,
            CancellationToken cancellationToken = default)
        {
            var pharmacyExists = await context.Pharmacies.AnyAsync(p => p.PharmacyId == pharmacyId, cancellationToken);
            if (!pharmacyExists)
                return Result.Failure(PharmacyErrors.PharmacyNotFound);

            var admin = await context.PharmacyAdmins.FirstOrDefaultAsync(a => a.Id == userId, cancellationToken);
            if (admin is null)
                return Result.Failure(PharmacyOwnerErrors.InvalidUserRole);

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
