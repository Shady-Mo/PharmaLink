namespace Infrastructure.Services.Pharmacist;

public class PharmacistManagementService(
    AppDbContext context,
    UserManager<AppUser> userManager,
    ILogger<PharmacistManagementService> logger) : IPharmacistManagementService
{
    public async Task<Result<PharmacistResponseDTO>> CreatePharmacistAsync(
        Guid adminId,
        CreatePharmacistRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        var admin = await context.PharmacyAdmins
            .Include(a => a.Pharmacy)
            .FirstOrDefaultAsync(a => a.Id == adminId, cancellationToken);
        if (admin?.PharmacyId is null)
        {
            logger.LogWarning("Pharmacy admin {AdminId} attempted to create pharmacist but is not assigned to any pharmacy.", adminId);
            return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.AdminNotAssignedToPharmacy);
        }

        var existingByEmail = await userManager.FindByEmailAsync(request.Email);
        if (existingByEmail is not null && existingByEmail.Status == UserStatus.Active)
        {
            logger.LogWarning("Pharmacy admin tried to create pharmacist with existing email: {Email}", request.Email);
            return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.EmailAlreadyExists);
        }

        var existingByPhone = await context.AppUsers
            .FirstOrDefaultAsync(p => p.PhoneNumber == request.PhoneNumber, cancellationToken);
        if (existingByPhone is not null && existingByPhone.Status == UserStatus.Active)
        {
            logger.LogWarning("Pharmacy admin tried to create pharmacist with existing phone: {Phone}", request.PhoneNumber);
            return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.PhoneAlreadyExists);
        }

        if (existingByEmail is not null && existingByEmail.Status == UserStatus.Inactive)
        {
            existingByEmail.Status = UserStatus.Active;
            existingByEmail.FullName = request.FullName;
            existingByEmail.PhoneNumber = request.PhoneNumber;
            var updateResult = await userManager.UpdateAsync(existingByEmail);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                logger.LogError("Failed to reactivate pharmacist {Id}. Errors: {Errors}", existingByEmail.Id, errors);
                return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.RegistrationFailed);
            }
            var pharmacyAssignment = new PharmacistAssignment {
                PharmacistId = existingByEmail.Id,
                PharmacyId = admin.PharmacyId.Value,
                AssignedByPharmacyAdminId = adminId,
                AssignedAt = DateTime.UtcNow,
                IsActive = true
            };
            await context.PharmacistAssignments.AddAsync(pharmacyAssignment, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Reactivated pharmacist account by admin. UserId: {UserId}", existingByEmail.Id);

            var pharmacistResponseDTO = new PharmacistResponseDTO {
                PharmacistId = existingByEmail.Id,
                FullName = existingByEmail.FullName,
                Email = existingByEmail.Email!,
                PhoneNumber = existingByEmail.PhoneNumber,
                CreatedAt = existingByEmail.CreatedAt,
                Status = existingByEmail.Status.ToString(),
                PharmacyLegalName = admin.Pharmacy!.LegalName
            };

            return Result.Success(pharmacistResponseDTO);
        }

        if (existingByPhone is not null && existingByPhone.Status == UserStatus.Inactive)
        {
            existingByPhone.Status = UserStatus.Active;
            existingByPhone.FullName = request.FullName;
            existingByPhone.Email = request.Email.ToLowerInvariant();
            existingByPhone.UserName = request.Email.ToLowerInvariant();
            var updateResult = await userManager.UpdateAsync(existingByPhone);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                logger.LogError("Failed to reactivate pharmacist {Id}. Errors: {Errors}", existingByPhone.Id, errors);
                return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.RegistrationFailed);
            }
            var pharmacyAssignment = new PharmacistAssignment {
                PharmacistId = existingByPhone.Id,
                PharmacyId = admin.PharmacyId.Value,
                AssignedByPharmacyAdminId = adminId,
                AssignedAt = DateTime.UtcNow,
                IsActive = true
            };
            await context.PharmacistAssignments.AddAsync(pharmacyAssignment, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Reactivated pharmacist account by admin. UserId: {UserId}", existingByPhone.Id);

            var pharmacistResponseDTO = new PharmacistResponseDTO {
                PharmacistId = existingByPhone.Id,
                FullName = existingByPhone.FullName,
                Email = existingByPhone.Email,
                PhoneNumber = existingByPhone.PhoneNumber!,
                CreatedAt = existingByPhone.CreatedAt,
                Status = existingByPhone.Status.ToString(),
                PharmacyLegalName = admin.Pharmacy!.LegalName
            };

            return Result.Success(pharmacistResponseDTO);
        }

        else
        {
            var pharmacist = request.Adapt<Domain.Entities.Pharmacist>();

            pharmacist.PhoneNumberConfirmed = true;
            pharmacist.EmailConfirmed = true;

            var createResult = await userManager.CreateAsync(pharmacist, request.Password);
            if (!createResult.Succeeded) {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                logger.LogError("Identity failed to create pharmacist. Errors: {Errors}", errors);
                return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.RegistrationFailed);
            }

            var roleResult = await userManager.AddToRoleAsync(pharmacist, AppRoles.Pharmacist);
            if (!roleResult.Succeeded) {
                await userManager.DeleteAsync(pharmacist);
                var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                logger.LogError("Failed to assign Pharmacist role. Rolled back. Errors: {Errors}", errors);
                return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.RegistrationFailed);
            }

            var assignment = new PharmacistAssignment {
                PharmacistId = pharmacist.Id,
                PharmacyId = admin.PharmacyId.Value,
                AssignedByPharmacyAdminId = adminId,
                AssignedAt = DateTime.UtcNow,
                IsActive = true
            };
            await context.PharmacistAssignments.AddAsync(assignment, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Pharmacist account created and assigned by admin {AdminId}. UserId: {UserId}", adminId, pharmacist.Id);

            var pharmacistResponseDTO = new PharmacistResponseDTO {
                PharmacistId = pharmacist.Id,
                FullName = pharmacist.FullName,
                Email = pharmacist.Email,
                PhoneNumber = pharmacist.PhoneNumber!,
                CreatedAt = pharmacist.CreatedAt,
                Status = pharmacist.Status.ToString(),
                PharmacyLegalName = admin.Pharmacy!.LegalName
            };

            return Result.Success(pharmacistResponseDTO);
        }
    }

    public async Task<Result<PaginatedList<PharmacistSummaryDTO>>> GetAllPharmacistsAsync(
        Guid adminId,
        PaginatedRequest request,
        CancellationToken cancellationToken = default)
    {
        var admin = await context.PharmacyAdmins.FirstOrDefaultAsync(a => a.Id == adminId, cancellationToken);
        if (admin?.PharmacyId is null)
            return Result.Failure<PaginatedList<PharmacistSummaryDTO>>(PharmacistErrors.AdminNotAssignedToPharmacy);

        var query = context.Pharmacists
            .AsNoTracking()
            .Where(p => context.PharmacistAssignments.Any(a => a.PharmacistId == p.Id && a.PharmacyId == admin.PharmacyId))
            .Select(p => new PharmacistSummaryDTO
            {
                PharmacistId = p.Id,
                FullName = p.FullName,
                Email = p.Email ?? string.Empty,
                PhoneNumber = p.PhoneNumber ?? string.Empty,
            });

        var paginated = await query
            .ToPaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);

        return Result.Success(paginated);
    }

    public async Task<Result<PharmacistResponseDTO>> GetPharmacistByIdAsync(
        Guid adminId,
        Guid pharmacistId,
        CancellationToken cancellationToken = default)
    {
        var admin = await context.PharmacyAdmins
            .Include(x => x.Pharmacy)
            .FirstOrDefaultAsync(a => a.Id == adminId, cancellationToken);
        if (admin?.PharmacyId is null)
            return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.AdminNotAssignedToPharmacy);

        var pharmacist = await context.Pharmacists
            .AsNoTracking()
            .Where(p => context.PharmacistAssignments.Any(a => a.PharmacistId == p.Id && a.PharmacyId == admin.PharmacyId && a.IsActive))
            .FirstOrDefaultAsync(p => p.Id == pharmacistId, cancellationToken);

        if (pharmacist is null)
            return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.PharmacistNotFound);

        var pharmacistResponseDTO = new PharmacistResponseDTO {
            PharmacistId = pharmacist.Id,
            FullName = pharmacist.FullName,
            Email = pharmacist.Email,
            PhoneNumber = pharmacist.PhoneNumber!,
            CreatedAt = pharmacist.CreatedAt,
            Status = pharmacist.Status.ToString(),
            PharmacyLegalName = admin.Pharmacy!.LegalName
        };

        return Result.Success(pharmacistResponseDTO);
    }

    public async Task<Result<PharmacistResponseDTO>> UpdatePharmacistAsync(
        Guid adminId,
        Guid pharmacistId,
        UpdatePharmacistRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        var admin = await context.PharmacyAdmins
            .Include(x => x.Pharmacy)
            .FirstOrDefaultAsync(a => a.Id == adminId, cancellationToken);
        if (admin?.PharmacyId is null)
            return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.AdminNotAssignedToPharmacy);

        var existingByPhone = await context.AppUsers
            .FirstOrDefaultAsync(p => p.PhoneNumber == request.PhoneNumber, cancellationToken);
        if (existingByPhone is not null)
        {
            logger.LogWarning("Pharmacy admin tried to create pharmacist with existing phone: {Phone}", request.PhoneNumber);
            return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.PhoneAlreadyExists);
        }

        var pharmacist = await context.Pharmacists
            .Where(p => context.PharmacistAssignments.Any(a => a.PharmacistId == p.Id && a.PharmacyId == admin.PharmacyId && a.IsActive))
            .FirstOrDefaultAsync(p => p.Id == pharmacistId, cancellationToken);

        if (pharmacist is null)
            return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.PharmacistNotFound);

        pharmacist.FullName = request.FullName;
        pharmacist.PhoneNumber = request.PhoneNumber;

        var updateResult = await userManager.UpdateAsync(pharmacist);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
            logger.LogError("Failed to update pharmacist {Id}. Errors: {Errors}", pharmacistId, errors);
            return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.RegistrationFailed);
        }

        var pharmacistResponseDTO = new PharmacistResponseDTO {
            PharmacistId = pharmacist.Id,
            FullName = pharmacist.FullName,
            Email = pharmacist.Email,
            PhoneNumber = pharmacist.PhoneNumber!,
            CreatedAt = pharmacist.CreatedAt,
            Status = pharmacist.Status.ToString(),
            PharmacyLegalName = admin.Pharmacy!.LegalName
        };

        return Result.Success(pharmacistResponseDTO);
    }

    public async Task<Result> DeletePharmacistAsync(
        Guid adminId,
        Guid pharmacistId,
        CancellationToken cancellationToken = default)
    {
        var admin = await context.PharmacyAdmins.FirstOrDefaultAsync(a => a.Id == adminId, cancellationToken);
        if (admin?.PharmacyId is null)
            return Result.Failure(PharmacistErrors.AdminNotAssignedToPharmacy);

        var isEmployed = await context.PharmacistAssignments.AnyAsync(a => a.PharmacistId == pharmacistId && a.PharmacyId == admin.PharmacyId && a.IsActive, cancellationToken);
        if (!isEmployed)
            return Result.Failure(PharmacistErrors.PharmacistNotFound);

        var pharmacist = await userManager.FindByIdAsync(pharmacistId.ToString());
        if (pharmacist is null)
            return Result.Failure(PharmacistErrors.PharmacistNotFound);

        var activeAssignment = await context.PharmacistAssignments
            .FirstOrDefaultAsync(a => a.PharmacistId == pharmacistId && a.IsActive, cancellationToken);

        pharmacist.Status = UserStatus.Inactive;

        if (activeAssignment is not null)
        {
            activeAssignment.IsActive = false;
            activeAssignment.EndedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Pharmacist {Id} deleted by admin.", pharmacistId);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<AssignmentHistoryItemDTO>>> GetPharmacistHistoryAsync(
        Guid adminId,
        Guid pharmacistId,
        CancellationToken cancellationToken = default)
    {
        var admin = await context.PharmacyAdmins.FirstOrDefaultAsync(a => a.Id == adminId, cancellationToken);
        if (admin?.PharmacyId is null)
            return Result.Failure<IReadOnlyList<AssignmentHistoryItemDTO>>(PharmacistErrors.AdminNotAssignedToPharmacy);

        var isEmployed = await context.PharmacistAssignments.AnyAsync(a => a.PharmacistId == pharmacistId && a.PharmacyId == admin.PharmacyId && a.IsActive, cancellationToken);
        if (!isEmployed)
            return Result.Failure<IReadOnlyList<AssignmentHistoryItemDTO>>(PharmacistErrors.PharmacistNotFound);

        var history = await LoadHistoryAsync(pharmacistId, cancellationToken);

        return Result.Success<IReadOnlyList<AssignmentHistoryItemDTO>>(history);
    }

    private async Task<List<AssignmentHistoryItemDTO>> LoadHistoryAsync(
        Guid pharmacistId,
        CancellationToken cancellationToken)
    {
        var assignments = await context.PharmacistAssignments
            .AsNoTracking()
            .Include(a => a.Pharmacy)
            .Where(a => a.PharmacistId == pharmacistId)
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync(cancellationToken);

        return assignments.Adapt<List<AssignmentHistoryItemDTO>>();
    }
}
