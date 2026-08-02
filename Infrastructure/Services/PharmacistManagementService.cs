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
            logger.LogWarning(
                "Pharmacy admin {AdminId} attempted to create pharmacist but is not assigned to any pharmacy.",
                adminId);
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
            logger.LogWarning("Pharmacy admin tried to create pharmacist with existing phone: {Phone}",
                request.PhoneNumber);
            return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.PhoneAlreadyExists);
        }

        var existingBranch = await context.PharmacyBranches
            .FirstOrDefaultAsync(b => b.BranchId == request.BranchId && b.PharmacyId == admin.PharmacyId.Value, cancellationToken);

        if (existingBranch is null)
        {
            logger.LogWarning("Pharmacy admin tried to assign branch that was not found or does not belong to his pharmacy");
            return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.BranchNotFound);
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

            var pharmacyAssignment = new PharmacistAssignment
            {
                PharmacistId = existingByEmail.Id,
                PharmacyId = admin.PharmacyId.Value,
                BranchId = existingBranch.BranchId,
                AssignedByPharmacyAdminId = adminId,
                AssignedAt = DateTime.UtcNow,
                IsActive = true
            };
            await context.PharmacistAssignments.AddAsync(pharmacyAssignment, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Reactivated pharmacist account by admin. UserId: {UserId}", existingByEmail.Id);

            var pharmacistResponseDTO = new PharmacistResponseDTO
            {
                PharmacistId = existingByEmail.Id,
                FullName = existingByEmail.FullName,
                Email = existingByEmail.Email!,
                PhoneNumber = existingByEmail.PhoneNumber!,
                CreatedAt = existingByEmail.CreatedAt,
                Status = existingByEmail.Status.ToString(),
                PharmacyLegalName = admin.Pharmacy!.LegalName,
                BranchId = existingBranch.BranchId,
                BranchName = existingBranch.BranchName,
                BranchCity = existingBranch.City,
                BranchAddress = $"{existingBranch.Governorate}، {existingBranch.City}، {existingBranch.AddressLine}",
                BranchPhone = existingBranch.PhoneNumber
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

            var pharmacyAssignment = new PharmacistAssignment
            {
                PharmacistId = existingByPhone.Id,
                PharmacyId = admin.PharmacyId.Value,
                AssignedByPharmacyAdminId = adminId,
                BranchId = existingBranch.BranchId,
                AssignedAt = DateTime.UtcNow,
                IsActive = true
            };
            await context.PharmacistAssignments.AddAsync(pharmacyAssignment, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Reactivated pharmacist account by admin. UserId: {UserId}", existingByPhone.Id);

            var pharmacistResponseDTO = new PharmacistResponseDTO
            {
                PharmacistId = existingByPhone.Id,
                FullName = existingByPhone.FullName,
                Email = existingByPhone.Email!,
                PhoneNumber = existingByPhone.PhoneNumber!,
                CreatedAt = existingByPhone.CreatedAt,
                Status = existingByPhone.Status.ToString(),
                PharmacyLegalName = admin.Pharmacy!.LegalName,
                BranchId = existingBranch.BranchId,
                BranchName = existingBranch.BranchName,
                BranchCity = existingBranch.City,
                BranchAddress = $"{existingBranch.Governorate}، {existingBranch.City}، {existingBranch.AddressLine}",
                BranchPhone = existingBranch.PhoneNumber
            };

            return Result.Success(pharmacistResponseDTO);
        }
        else
        {
            var pharmacist = request.Adapt<Domain.Entities.Pharmacist>();

            pharmacist.PhoneNumberConfirmed = true;
            pharmacist.EmailConfirmed = true;

            var createResult = await userManager.CreateAsync(pharmacist, request.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                logger.LogError("Identity failed to create pharmacist. Errors: {Errors}", errors);
                return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.RegistrationFailed);
            }

            var roleResult = await userManager.AddToRoleAsync(pharmacist, AppRoles.Pharmacist);
            if (!roleResult.Succeeded)
            {
                await userManager.DeleteAsync(pharmacist);
                var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                logger.LogError("Failed to assign Pharmacist role. Rolled back. Errors: {Errors}", errors);
                return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.RegistrationFailed);
            }

            var assignment = new PharmacistAssignment
            {
                PharmacistId = pharmacist.Id,
                PharmacyId = admin.PharmacyId.Value,
                AssignedByPharmacyAdminId = adminId,
                AssignedAt = DateTime.UtcNow,
                BranchId = existingBranch.BranchId,
                IsActive = true
            };
            await context.PharmacistAssignments.AddAsync(assignment, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Pharmacist account created and assigned by admin {AdminId}. UserId: {UserId}",
                adminId, pharmacist.Id);

            var pharmacistResponseDTO = new PharmacistResponseDTO
            {
                PharmacistId = pharmacist.Id,
                FullName = pharmacist.FullName,
                Email = pharmacist.Email!,
                PhoneNumber = pharmacist.PhoneNumber!,
                CreatedAt = pharmacist.CreatedAt,
                Status = pharmacist.Status.ToString(),
                PharmacyLegalName = admin.Pharmacy!.LegalName,
                BranchId = existingBranch.BranchId,
                BranchName = existingBranch.BranchName,
                BranchCity = existingBranch.City,
                BranchAddress = $"{existingBranch.Governorate}، {existingBranch.City}، {existingBranch.AddressLine}",
                BranchPhone = existingBranch.PhoneNumber
            };

            return Result.Success(pharmacistResponseDTO);
        }
    }

    public async Task<Result<PaginatedList<PharmacistSummaryDTO>>> GetAllPharmacistsAsync(
        Guid adminId,
        GetAllPharmacistsRequestDTO request,
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
                Status = p.Status,
                ActiveBranchName = context.PharmacistAssignments
                    .Where(a => a.PharmacistId == p.Id && a.IsActive && a.PharmacyId == admin.PharmacyId)
                    .Select(a => a.Branch.BranchName)
                    .FirstOrDefault(),

                Assignments = context.PharmacistAssignments
                    .Where(a => a.PharmacistId == p.Id && a.PharmacyId == admin.PharmacyId)
                    .Select(a => new AssignmentDTO
                    {
                        PharmacistId = a.PharmacistId,
                        PharmacyId = a.PharmacyId,
                        BranchId = a.BranchId,
                        AssignedAt = a.AssignedAt,
                        EndedAt = a.EndedAt,
                        AssignedByPharmacyAdminId = a.AssignedByPharmacyAdminId,
                        IsActive = a.IsActive
                    })
                    .ToList()
            });

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(p => p.FullName.ToLower().Contains(term) || p.Email.ToLower().Contains(term));
        }

        if (request.BranchId.HasValue)
        {
            query = query.Where(p => p.Assignments != null && p.Assignments.Any(a => a.BranchId == request.BranchId));
        }
        if (request.userStatus.HasValue)
        {
            query = query.Where(p => p.Status == request.userStatus);
        }

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

        var hasAssignment = await context.PharmacistAssignments
            .AnyAsync(a => a.PharmacistId == pharmacistId && a.PharmacyId == admin.PharmacyId, cancellationToken);
        if (!hasAssignment)
            return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.PharmacistNotFound);

        var pharmacist = await context.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == pharmacistId, cancellationToken);

        if (pharmacist is null)
            return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.PharmacistNotFound);

        var activeBranchInfo = await context.PharmacistAssignments
            .AsNoTracking()
            .Where(a => a.PharmacistId == pharmacist.Id && a.PharmacyId == admin.PharmacyId && a.IsActive)
            .Select(a => new
            {
                a.BranchId,
                BranchName = a.Branch != null ? a.Branch.BranchName : null,
                City = a.Branch != null ? a.Branch.City : null,
                Address = a.Branch != null ? (a.Branch.Governorate + "، " + a.Branch.City + "، " + a.Branch.AddressLine) : null,
                Phone = a.Branch != null ? a.Branch.PhoneNumber : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        var pharmacistResponseDTO = new PharmacistResponseDTO
        {
            PharmacistId = pharmacist.Id,
            FullName = pharmacist.FullName,
            Email = pharmacist.Email!,
            PhoneNumber = pharmacist.PhoneNumber!,
            CreatedAt = pharmacist.CreatedAt,
            Status = pharmacist.Status.ToString(),
            PharmacyLegalName = admin.Pharmacy!.LegalName,
            BranchId = activeBranchInfo?.BranchId ?? Guid.Empty,
            BranchName = activeBranchInfo?.BranchName,
            BranchCity = activeBranchInfo?.City,
            BranchAddress = activeBranchInfo?.Address,
            BranchPhone = activeBranchInfo?.Phone
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

        var hasAssignment = await context.PharmacistAssignments
            .AnyAsync(a => a.PharmacistId == pharmacistId && a.PharmacyId == admin.PharmacyId, cancellationToken);
        if (!hasAssignment)
            return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.PharmacistNotFound);

        var pharmacist = await context.AppUsers
            .FirstOrDefaultAsync(p => p.Id == pharmacistId, cancellationToken);

        if (pharmacist is null)
            return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.PharmacistNotFound);

        if (pharmacist.PhoneNumber != request.PhoneNumber)
        {
            var existingByPhone = await context.AppUsers
                .FirstOrDefaultAsync(p => p.PhoneNumber == request.PhoneNumber && p.Id != pharmacistId, cancellationToken);
            if (existingByPhone is not null)
            {
                logger.LogWarning("Pharmacy admin tried to update pharmacist with existing phone: {Phone}",
                    request.PhoneNumber);
                return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.PhoneAlreadyExists);
            }
        }

        pharmacist.FullName = request.FullName;
        pharmacist.PhoneNumber = request.PhoneNumber;

        var updateResult = await userManager.UpdateAsync(pharmacist);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
            logger.LogError("Failed to update pharmacist {Id}. Errors: {Errors}", pharmacistId, errors);
            return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.RegistrationFailed);
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(pharmacist);
            var resetResult = await userManager.ResetPasswordAsync(pharmacist, token, request.Password);
            if (!resetResult.Succeeded)
            {
                var errors = string.Join("; ", resetResult.Errors.Select(e => e.Description));
                logger.LogError("Failed to reset password for pharmacist {Id}. Errors: {Errors}", pharmacistId, errors);
                return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.RegistrationFailed);
            }
        }

        var activeBranchInfo = await context.PharmacistAssignments
            .AsNoTracking()
            .Where(a => a.PharmacistId == pharmacist.Id && a.PharmacyId == admin.PharmacyId && a.IsActive)
            .Select(a => new
            {
                a.BranchId,
                BranchName = a.Branch != null ? a.Branch.BranchName : null,
                City = a.Branch != null ? a.Branch.City : null,
                Address = a.Branch != null ? (a.Branch.Governorate + "، " + a.Branch.City + "، " + a.Branch.AddressLine) : null,
                Phone = a.Branch != null ? a.Branch.PhoneNumber : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        var pharmacistResponseDTO = new PharmacistResponseDTO
        {
            PharmacistId = pharmacist.Id,
            FullName = pharmacist.FullName,
            Email = pharmacist.Email!,
            PhoneNumber = pharmacist.PhoneNumber!,
            CreatedAt = pharmacist.CreatedAt,
            Status = pharmacist.Status.ToString(),
            PharmacyLegalName = admin.Pharmacy!.LegalName,
            BranchId = activeBranchInfo?.BranchId ?? Guid.Empty,
            BranchName = activeBranchInfo?.BranchName,
            BranchCity = activeBranchInfo?.City,
            BranchAddress = activeBranchInfo?.Address,
            BranchPhone = activeBranchInfo?.Phone
        };

        return Result.Success(pharmacistResponseDTO);
    }

    public async Task<Result<PharmacistResponseDTO>> UpdatePharmacistStatusAsync(
        Guid adminId,
        Guid pharmacistId,
        UserStatus status,
        CancellationToken cancellationToken = default)
    {
        var admin = await context.PharmacyAdmins
            .Include(x => x.Pharmacy)
            .FirstOrDefaultAsync(a => a.Id == adminId, cancellationToken);
        if (admin?.PharmacyId is null)
            return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.AdminNotAssignedToPharmacy);

        var hasAssignment = await context.PharmacistAssignments
            .AnyAsync(a => a.PharmacistId == pharmacistId && a.PharmacyId == admin.PharmacyId, cancellationToken);
        if (!hasAssignment)
            return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.PharmacistNotFound);

        var pharmacist = await context.AppUsers
            .FirstOrDefaultAsync(p => p.Id == pharmacistId, cancellationToken);

        if (pharmacist is null)
            return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.PharmacistNotFound);

        pharmacist.Status = status;
        await context.SaveChangesAsync(cancellationToken);

        var activeBranchInfo = await context.PharmacistAssignments
            .AsNoTracking()
            .Where(a => a.PharmacistId == pharmacist.Id && a.PharmacyId == admin.PharmacyId && a.IsActive)
            .Select(a => new
            {
                a.BranchId,
                BranchName = a.Branch != null ? a.Branch.BranchName : null,
                City = a.Branch != null ? a.Branch.City : null,
                Address = a.Branch != null ? (a.Branch.Governorate + "، " + a.Branch.City + "، " + a.Branch.AddressLine) : null,
                Phone = a.Branch != null ? a.Branch.PhoneNumber : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        var pharmacistResponseDTO = new PharmacistResponseDTO
        {
            PharmacistId = pharmacist.Id,
            FullName = pharmacist.FullName,
            Email = pharmacist.Email!,
            PhoneNumber = pharmacist.PhoneNumber!,
            CreatedAt = pharmacist.CreatedAt,
            Status = pharmacist.Status.ToString(),
            PharmacyLegalName = admin.Pharmacy!.LegalName,
            BranchId = activeBranchInfo?.BranchId ?? Guid.Empty,
            BranchName = activeBranchInfo?.BranchName,
            BranchCity = activeBranchInfo?.City,
            BranchAddress = activeBranchInfo?.Address,
            BranchPhone = activeBranchInfo?.Phone
        };

        return Result.Success(pharmacistResponseDTO);
    }

    public async Task<Result<PharmacistResponseDTO>> AssignBranchAsync(
        Guid adminId,
        Guid pharmacistId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        var admin = await context.PharmacyAdmins
            .Include(x => x.Pharmacy)
            .FirstOrDefaultAsync(a => a.Id == adminId, cancellationToken);
        if (admin?.PharmacyId is null)
            return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.AdminNotAssignedToPharmacy);

        var hasAssignment = await context.PharmacistAssignments
            .AnyAsync(a => a.PharmacistId == pharmacistId && a.PharmacyId == admin.PharmacyId, cancellationToken);
        if (!hasAssignment)
            return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.PharmacistNotFound);

        var targetBranch = await context.PharmacyBranches
            .FirstOrDefaultAsync(b => b.BranchId == branchId && b.PharmacyId == admin.PharmacyId.Value, cancellationToken);
        if (targetBranch is null)
            return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.BranchNotFound);

        var activeAssignment = await context.PharmacistAssignments
            .FirstOrDefaultAsync(a => a.PharmacistId == pharmacistId && a.PharmacyId == admin.PharmacyId && a.IsActive, cancellationToken);

        if (activeAssignment is not null && activeAssignment.BranchId == branchId)
        {
            return Result.Failure<PharmacistResponseDTO>(PharmacistErrors.AlreadyAssignedToBranch);
        }

        if (activeAssignment is not null)
        {
            activeAssignment.IsActive = false;
            activeAssignment.EndedAt = DateTime.UtcNow;
        }

        var newAssignment = new PharmacistAssignment
        {
            PharmacistId = pharmacistId,
            PharmacyId = admin.PharmacyId.Value,
            BranchId = branchId,
            AssignedByPharmacyAdminId = adminId,
            AssignedAt = DateTime.UtcNow,
            IsActive = true
        };
        await context.PharmacistAssignments.AddAsync(newAssignment, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return await GetPharmacistByIdAsync(adminId, pharmacistId, cancellationToken);
    }

    public async Task<Result> DeletePharmacistAsync(
        Guid adminId,
        Guid pharmacistId,
        CancellationToken cancellationToken = default)
    {
        var admin = await context.PharmacyAdmins.FirstOrDefaultAsync(a => a.Id == adminId, cancellationToken);
        if (admin?.PharmacyId is null)
            return Result.Failure(PharmacistErrors.AdminNotAssignedToPharmacy);

        var isEmployed = await context.PharmacistAssignments.AnyAsync(
            a => a.PharmacistId == pharmacistId && a.PharmacyId == admin.PharmacyId && a.IsActive, cancellationToken);
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

        var hasAnyAssignment = await context.PharmacistAssignments.AnyAsync(
            a => a.PharmacistId == pharmacistId && a.PharmacyId == admin.PharmacyId, cancellationToken);
        if (!hasAnyAssignment)
            return Result.Failure<IReadOnlyList<AssignmentHistoryItemDTO>>(PharmacistErrors.PharmacistNotFound);

        var history = await LoadHistoryAsync(pharmacistId, cancellationToken);

        return Result.Success<IReadOnlyList<AssignmentHistoryItemDTO>>(history);
    }

    private async Task<List<AssignmentHistoryItemDTO>> LoadHistoryAsync(
        Guid pharmacistId,
        CancellationToken cancellationToken)
    {
        return await context.PharmacistAssignments
            .AsNoTracking()
            .Where(a => a.PharmacistId == pharmacistId)
            .OrderByDescending(a => a.AssignedAt)
            .Select(a => new AssignmentHistoryItemDTO
            {
                AssignmentId = a.Id,
                PharmacistId = a.PharmacistId,
                PharmacyId = a.PharmacyId,
                PharmacyLegalName = a.Pharmacy != null ? a.Pharmacy.LegalName : string.Empty,
                BranchId = a.BranchId,
                BranchName = a.Branch != null ? a.Branch.BranchName : string.Empty,
                AssignedAt = a.AssignedAt,
                EndedAt = a.EndedAt,
                AssignedByAdminId = a.AssignedByPharmacyAdminId,
                AssignedByAdminFullName = a.AssignedByPharmacyAdmin != null ? a.AssignedByPharmacyAdmin.FullName : "أدمن الصيدلية",
                IsActive = a.IsActive
            })
            .ToListAsync(cancellationToken);
    }
}