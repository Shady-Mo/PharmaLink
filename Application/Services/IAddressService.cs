using Application.DTOs.Addresses.Requests;
using Application.DTOs.Addresses.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{

    public interface IAddressService
    {
        Task<Result<AddressResponseDTO>> CreateAsync(
            Guid patientId, CreateAddressRequestDTO request, CancellationToken cancellationToken = default);

        Task<Result<List<AddressResponseDTO>>> GetAllForPatientAsync(
            Guid patientId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Patients can read only their own address (403 otherwise).
        /// System Admins can read any address but must supply an audit reason;
        /// every such read is written to the audit log.
        /// </summary>
        Task<Result<AddressResponseDTO>> GetByIdAsync(
            Guid addressId, Guid requestingUserId, string requestingRole,
            string? auditReason, CancellationToken cancellationToken = default);

        Task<Result<AddressResponseDTO>> UpdateAsync(
            Guid addressId, Guid patientId, UpdateAddressRequestDTO request,
            CancellationToken cancellationToken = default);

        Task<Result> DeleteAsync(
            Guid addressId, Guid patientId, CancellationToken cancellationToken = default);

        /// <summary>Atomically sets this address as default and unsets all others for the patient.</summary>
        Task<Result> SetDefaultAsync(
            Guid addressId, Guid patientId, CancellationToken cancellationToken = default);
    }
}
