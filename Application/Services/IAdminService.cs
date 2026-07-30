namespace Application.Services
{
    public interface IAdminService
    {
        Task<Result<AdminProfileResponseDto>> GetProfileAsync(Guid adminId, CancellationToken cancellationToken = default);
        Task<Result<AdminProfileResponseDto>> UpdateProfileAsync(Guid adminId, UpdateAdminProfileDto updateDto, CancellationToken cancellationToken = default);
    }
}
