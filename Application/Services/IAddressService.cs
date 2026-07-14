using Application.DTOs.Addresses.Requests;
using Application.DTOs.Addresses.Response;
using Domain.Constants;

namespace Application.Services;

public interface IAddressService
{
    Task<Result<AddressResponseDTO>> CreateAsync(
        Guid patientId, CreateAddressRequestDTO request, CancellationToken cancellationToken = default);

    Task<Result<List<AddressResponseDTO>>> GetAllForPatientAsync(
        Guid patientId, CancellationToken cancellationToken = default);

    Task<Result<List<AddressResponseDTO>>> GetAllAddressesByAdminAsync(
        CancellationToken cancellationToken = default);


    Task<Result<AddressResponseDTO>> GetByIdAsync(
        Guid addressId, Guid patientId,string roleName, CancellationToken cancellationToken = default);

   
    Task<Result<AddressResponseDTO>> UpdateAsync(
        Guid addressId, Guid patientId, UpdateAddressRequestDTO request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        Guid addressId, Guid patientId, CancellationToken cancellationToken = default);

    Task<Result> SetDefaultAsync(
        Guid addressId, Guid patientId, CancellationToken cancellationToken = default);
}