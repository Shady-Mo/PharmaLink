using Application.DTOs.Admin;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Services
{
    public class AdminService(
    AppDbContext context,
    ILogger<AdminService> logger) : IAdminService
    {
        public async Task<Result<AdminProfileResponseDto>> GetProfileAsync(Guid adminId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var admin = await context.AppUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == adminId, cancellationToken);

            if (admin is null)
            {
                logger.LogWarning("Profile fetch failed: System admin with ID {AdminId} was not found.", adminId);
                return Result.Failure<AdminProfileResponseDto>(AdminErrors.AdminNotFound);
            }

            var profileDto = MapToProfileDto(admin);
            return Result.Success(profileDto);
        }

        public async Task<Result<AdminProfileResponseDto>> UpdateProfileAsync(Guid adminId, UpdateAdminProfileDto updateDto, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var existingByPhone = await context.AppUsers
                .FirstOrDefaultAsync(u => u.PhoneNumber == updateDto.PhoneNumber && u.Id != adminId, cancellationToken);

            if (existingByPhone is not null)
            {
                logger.LogWarning("Admin tried to update profile with an existing phone: {Phone}", updateDto.PhoneNumber);
                return Result.Failure<AdminProfileResponseDto>(AdminErrors.PhoneAlreadyExists);
            }

            var admin = await context.AppUsers
                .FirstOrDefaultAsync(u => u.Id == adminId, cancellationToken);

            if (admin is null)
            {
                logger.LogWarning("Profile update failed: Admin with ID {AdminId} was not found.", adminId);
                return Result.Failure<AdminProfileResponseDto>(AdminErrors.AdminNotFound);
            }

            admin.FullName = updateDto.FullName;
            admin.PhoneNumber = updateDto.PhoneNumber;

            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Admin profile updated successfully for ID {AdminId}.", adminId);

            return Result.Success(MapToProfileDto(admin));
        }

        private static AdminProfileResponseDto MapToProfileDto(Domain.Entities.AppUser admin)
        {
            return new AdminProfileResponseDto
            {
                AdminId = admin.Id,
                FullName = admin.FullName,
                Email = admin.Email ?? string.Empty,
                PhoneNumber = admin.PhoneNumber ?? string.Empty,
                Status = admin.Status.ToString(),
                CreatedAt = admin.CreatedAt
            };
        }
    }
}
