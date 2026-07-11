using System;
using System.Collections.Generic;
using System.Text;
using Application.DTOs.Addresses.Requests;
using Application.DTOs.Addresses.Response;
using Domain.Constants;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace Infrastructure.Services
{
  

    public class AddressService(
        AppDbContext dbContext) : IAddressService
    {
        private static readonly GeometryFactory GeoFactory =
            NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        public async Task<Result<AddressResponseDTO>> CreateAsync(
            Guid patientId, CreateAddressRequestDTO request, CancellationToken cancellationToken = default)
        {
            
            Address address = request.Adapt<Address>();
            address.UserId = patientId;
            address.GeoLocation = GeoFactory.CreatePoint(new Coordinate(request.Longitude, request.Latitude));
            if (request.IsDefault)
            {
                // AC: marking one as default atomically unsets IsDefault on all the patient's others.
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

                await dbContext.Addresses
                    .Where(a => a.UserId == patientId && a.IsDefault)
                    .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false), cancellationToken);

                dbContext.Addresses.Add(address);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {

                dbContext.Addresses.Add(address);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
           
            return Result.Success(MapToDto(address));
        }

        public async Task<Result<List<AddressResponseDTO>>> GetAllForPatientAsync(
            Guid patientId, CancellationToken cancellationToken = default)
        {
            var addresses = await dbContext.Addresses
                .AsNoTracking()
                .Where(a => a.UserId == patientId)
                .ToListAsync(cancellationToken);

            return Result.Success(addresses.Select(MapToDto).ToList());
        }

        public async Task<Result<AddressResponseDTO>> GetByIdAsync(
            Guid addressId, Guid requestingUserId, string requestingRole,
            string? auditReason, CancellationToken cancellationToken = default)
        {
            var address = await dbContext.Addresses
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AddressId == addressId, cancellationToken);

            if (address is null)
                return Result.Failure<AddressResponseDTO>(AddressErrors.NotFound);

            if (requestingRole == AppRoles.Patient)
            {
                // AC: a Patient may only access their own address; otherwise 403.
                if (address.UserId != requestingUserId)
                    return Result.Failure<AddressResponseDTO>(AddressErrors.Forbidden);
            }
            else if (requestingRole == AppRoles.Admin)
            {
                if (string.IsNullOrWhiteSpace(auditReason))
                    return Result.Failure<AddressResponseDTO>(AddressErrors.AuditReasonRequired);

                
            }
            else
            {
                return Result.Failure<AddressResponseDTO>(AddressErrors.Forbidden);
            }

            return Result.Success(MapToDto(address));
        }

        public async Task<Result<AddressResponseDTO>> UpdateAsync(
            Guid addressId, Guid patientId, UpdateAddressRequestDTO request,
            CancellationToken cancellationToken = default)
        {
            var address = await dbContext.Addresses
                .FirstOrDefaultAsync(a => a.AddressId == addressId, cancellationToken);

            if (address is null)
                return Result.Failure<AddressResponseDTO>(AddressErrors.NotFound);

            if (address.UserId != patientId)
                return Result.Failure<AddressResponseDTO>(AddressErrors.Forbidden);

            address.Label = request.Label;
            address.AddressLine = request.AddressLine;
            address.City = request.City;
            address.Governorate = request.Governorate;
            address.GeoLocation = GeoFactory.CreatePoint(new Coordinate(request.Longitude, request.Latitude));

            if (request.IsDefault && !address.IsDefault)
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

                await dbContext.Addresses
                    .Where(a => a.UserId == patientId && a.AddressId != addressId && a.IsDefault)
                    .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false), cancellationToken);

                address.IsDefault = true;
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                address.IsDefault = request.IsDefault;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return Result.Success(MapToDto(address));
        }

        public async Task<Result> DeleteAsync(
    Guid addressId, Guid patientId, CancellationToken cancellationToken = default)
        {
            var address = await dbContext.Addresses
                .FirstOrDefaultAsync(a => a.AddressId == addressId, cancellationToken);

            if (address is null)
                return Result.Failure(AddressErrors.NotFound);

            if (address.UserId != patientId)
                return Result.Failure(AddressErrors.Forbidden);

            // AC doesn't cover this case explicitly, but Orders.DeliveryAddressId is Restrict —
            // check up front so we return a clean 409 instead of letting a DbUpdateException surface.
            var isReferencedByOrder = await dbContext.Orders
                .AnyAsync(o => o.DeliveryAddressId == addressId, cancellationToken);

            if (isReferencedByOrder)
                return Result.Failure(AddressErrors.InUse);

            dbContext.Addresses.Remove(address);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        public async Task<Result> SetDefaultAsync(
            Guid addressId, Guid patientId, CancellationToken cancellationToken = default)
        {
            var address = await dbContext.Addresses
                .FirstOrDefaultAsync(a => a.AddressId == addressId, cancellationToken);

            if (address is null)
                return Result.Failure(AddressErrors.NotFound);

            if (address.UserId != patientId)
                return Result.Failure(AddressErrors.Forbidden);
            if (address.IsDefault)
                return Result.Failure(AddressErrors.AddressAlreadyDefault);
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            await dbContext.Addresses
                .Where(a => a.UserId == patientId && a.AddressId != addressId && a.IsDefault)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false), cancellationToken);

            address.IsDefault = true;
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result.Success();
        }

        private static AddressResponseDTO MapToDto(Address address) => new()
        {
            AddressId = address.AddressId,
            UserId=address.UserId,
            Label = address.Label,
            AddressLine = address.AddressLine,
            City = address.City,
            Governorate = address.Governorate,
           Longitude=address.GeoLocation?.X??0,
           Latitude=address.GeoLocation?.Y??0,
            IsDefault = address.IsDefault
        };
    }
}
