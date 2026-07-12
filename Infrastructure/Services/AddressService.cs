using Application.DTOs.Addresses.Requests;
using Application.DTOs.Addresses.Response;
using NetTopologySuite;

namespace Infrastructure.Services;

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
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            await UnsetOtherDefaultAddressesAsync(patientId, null, cancellationToken);
            
            dbContext.Addresses.Add(address);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        else
        {
            dbContext.Addresses.Add(address);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(address.Adapt<AddressResponseDTO>());
    }

    public async Task<Result<List<AddressResponseDTO>>> GetAllForPatientAsync(
        Guid patientId, CancellationToken cancellationToken = default)
    {
        var addresses = await dbContext.Addresses
            .AsNoTracking()
            .Where(a => a.UserId == patientId)
            .ProjectToType<AddressResponseDTO>()
            .ToListAsync(cancellationToken);

        return Result.Success(addresses);
    }

    public async Task<Result<AddressResponseDTO>> GetByIdAsync(
        Guid addressId, Guid patientId, CancellationToken cancellationToken = default)
    {
        var address = await dbContext.Addresses
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AddressId == addressId, cancellationToken);

        if (address is null)
            return Result.Failure<AddressResponseDTO>(AddressErrors.NotFound);

        if (address.UserId != patientId)
            return Result.Failure<AddressResponseDTO>(AddressErrors.Forbidden);

        return Result.Success(address.Adapt<AddressResponseDTO>());
    }

    public async Task<Result<AddressResponseDTO>> GetByIdForAdminAsync(
        Guid addressId, string? auditReason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(auditReason))
            return Result.Failure<AddressResponseDTO>(AddressErrors.AuditReasonRequired);

        var address = await dbContext.Addresses
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AddressId == addressId, cancellationToken);

        if (address is null)
            return Result.Failure<AddressResponseDTO>(AddressErrors.NotFound);

        return Result.Success(address.Adapt<AddressResponseDTO>());
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
            await UnsetOtherDefaultAddressesAsync(patientId, addressId, cancellationToken);
            
            address.IsDefault = true;
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        else
        {
            address.IsDefault = request.IsDefault;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(address.Adapt<AddressResponseDTO>());
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
        await UnsetOtherDefaultAddressesAsync(patientId, addressId, cancellationToken);

        address.IsDefault = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }

    private async Task UnsetOtherDefaultAddressesAsync(Guid patientId, Guid? excludeAddressId, CancellationToken cancellationToken)
    {
        var query = dbContext.Addresses.Where(a => a.UserId == patientId && a.IsDefault);
        
        if (excludeAddressId.HasValue)
        {
            query = query.Where(a => a.AddressId != excludeAddressId.Value);
        }

        await query.ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false), cancellationToken);
    }
}