namespace Infrastructure.Services;

public class PharmacyBranchService(
    AppDbContext context,
    ILogger<PharmacyBranchService> logger) : IPharmacyBranchService
{
    public async Task<Result<GetPharmacyBranchResponseDTO>> CreateAsync(
        Guid pharmacyId,
        CreatePharmacyBranchDTO dto,
        CancellationToken cancellationToken = default)
    {
        var pharmacyExists = await context.Pharmacies
            .AnyAsync(p => p.PharmacyId == pharmacyId, cancellationToken);

        if (!pharmacyExists)
            return Result.Failure<GetPharmacyBranchResponseDTO>(PharmacyErrors.PharmacyNotFound);

        var nameConflict = await context.PharmacyBranches
            .AnyAsync(b => b.PharmacyId == pharmacyId &&
                           b.BranchName == dto.BranchName,
                cancellationToken);

        if (nameConflict)
            return Result.Failure<GetPharmacyBranchResponseDTO>(PharmacyBranchErrors.DuplicateBranchName);

        var coordsResult = BuildPoint(dto.Latitude, dto.Longitude);
        if (coordsResult.IsFailure)
            return Result.Failure<GetPharmacyBranchResponseDTO>(coordsResult.Error);

        var branch = dto.Adapt<PharmacyBranch>();

        branch.PharmacyId = pharmacyId;
        branch.GeoLocation = coordsResult.Value;

        context.PharmacyBranches.Add(branch);
        await context.SaveChangesAsync(cancellationToken);

        var allDrugs = await context.Drugs
            .Where(d => d.IsActive)
            .Select(d => new { d.DrugId, d.FinalPrice })
            .ToListAsync(cancellationToken);

        var defaultInventories = allDrugs.Select(d => new PharmacyInventory
        {
            BranchId = branch.BranchId,
            DrugId = d.DrugId,
            StockQuantity = 10,
            ReservedQuantity = 0,
            UnitPrice = d.FinalPrice,
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            LastSyncedAt = DateTime.UtcNow,
            ReorderPoint = 2
        }).ToList();

        if (defaultInventories.Count != 0)
        {
            context.PharmacyInventories.AddRange(defaultInventories);
            await context.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Created branch {BranchId} for Pharmacy {PharmacyId}", branch.BranchId, pharmacyId);

        var branchDto = branch.Adapt<GetPharmacyBranchResponseDTO>();

        return Result.Success(branchDto);
    }

    public async Task<Result<PaginatedList<GetPharmacyBranchResponseDTO>>> GetAllAsync(
        Guid pharmacyId,
        GetPharmacyBranchParamRequest parameters,
        CancellationToken cancellationToken = default)
    {
        var branches = context.PharmacyBranches
            .AsNoTracking()
            .Where(b => b.PharmacyId == pharmacyId);

        if (!string.IsNullOrWhiteSpace(parameters.Search)) {
            var searchTerm = parameters.Search;

            branches = branches.Where(b =>
                b.BranchName.Contains(searchTerm) ||
                b.AddressLine.Contains(searchTerm));
        }

        var page = await branches
            .OrderBy(b => b.BranchName)
            .ProjectToType<GetPharmacyBranchResponseDTO>()
            .ToPaginatedListAsync(parameters.PageNumber, parameters.PageSize, cancellationToken);

        return Result.Success(page);
    }

    public async Task<Result<PharmacyBranchResponseDTO>> GetByIdAsync(
        Guid pharmacyId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        var branch = await context.PharmacyBranches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.BranchId == branchId && b.PharmacyId == pharmacyId,
                cancellationToken);

        if (branch is null)
            return Result.Failure<PharmacyBranchResponseDTO>(PharmacyBranchErrors.BranchNotFound);

        var branchDto = branch.Adapt<PharmacyBranchResponseDTO>();

        return Result.Success(branchDto);
    }

    public async Task<Result<GetPharmacyBranchResponseDTO>> UpdateAsync(
        Guid pharmacyId,
        Guid branchId,
        UpdatePharmacyBranchDTO dto,
        CancellationToken cancellationToken = default)
    {
        var branch = await context.PharmacyBranches
            .FirstOrDefaultAsync(b => b.BranchId == branchId && b.PharmacyId == pharmacyId,
                cancellationToken);

        if (branch is null)
            return Result.Failure<GetPharmacyBranchResponseDTO>(PharmacyBranchErrors.BranchNotFound);

        var nameConflict = await context.PharmacyBranches
            .AnyAsync(b => b.PharmacyId == pharmacyId &&
                           b.BranchId != branchId &&
                           b.BranchName == dto.BranchName.ToLowerInvariant(),
                cancellationToken);

        if (nameConflict)
            return Result.Failure<GetPharmacyBranchResponseDTO>(PharmacyBranchErrors.DuplicateBranchName);

        var coordsResult = BuildPoint(dto.Latitude, dto.Longitude);
        if (coordsResult.IsFailure)
            return Result.Failure<GetPharmacyBranchResponseDTO>(coordsResult.Error);

        branch = dto.Adapt(branch);

        branch.GeoLocation = coordsResult.Value ?? branch.GeoLocation;

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Updated branch {BranchId} for Pharmacy {PharmacyId}", branchId, pharmacyId);

        var branchDto = branch.Adapt<GetPharmacyBranchResponseDTO>();

        return Result.Success(branchDto);
    }

    public async Task<Result> DeleteAsync(
        Guid pharmacyId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        var branch = await context.PharmacyBranches
            .FirstOrDefaultAsync(b => b.BranchId == branchId && b.PharmacyId == pharmacyId,
                cancellationToken);

        if (branch is null)
            return Result.Failure(PharmacyBranchErrors.BranchNotFound);

        context.PharmacyBranches.Remove(branch);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Deleted branch {BranchId} from Pharmacy {PharmacyId}", branchId, pharmacyId);

        return Result.Success();
    }

    public async Task<Result<List<PharmacyBranchSearchDTO>>> SearchAsync(
        Guid pharmacyId,
        string? term,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(term))
            return Result.Success(new List<PharmacyBranchSearchDTO>());

        var searchResults = await context.PharmacyBranches
            .Where(b => b.PharmacyId == pharmacyId &&
                (b.BranchName.Contains(term) || b.AddressLine.Contains(term)))
            .ProjectToType<PharmacyBranchSearchDTO>()
            .Take(10)
            .ToListAsync(cancellationToken);

        return Result.Success(searchResults);
    }

    private static Result<Point?> BuildPoint(double? latitude, double? longitude)
    {
        var hasLat = latitude.HasValue;
        var hasLng = longitude.HasValue;

        if (!hasLat && !hasLng)
            return Result.Success<Point?>(null);

        if (hasLat != hasLng)
            return Result.Failure<Point?>(PharmacyBranchErrors.InvalidCoordinates);

        if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
            return Result.Failure<Point?>(PharmacyBranchErrors.InvalidCoordinates);

        return Result.Success<Point?>(new Point(longitude!.Value, latitude!.Value) { SRID = 4326 });
    }
}
