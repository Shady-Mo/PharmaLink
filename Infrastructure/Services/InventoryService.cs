namespace Infrastructure.Services;

public class InventoryService(
    AppDbContext context,
    ILogger<InventoryService> logger,
    IHttpContextAccessor httpContextAccessor) : IInventoryService
{
    private const int LowStockThreshold = 10;

    public async Task<Result> ReserveStockAsync(Guid branchId, Guid drugId, int quantity,
        CancellationToken cancellationToken = default)
    {
        var inventoryResult = await FindAndValidateInventoryAsync(branchId, drugId, quantity, isRelease: false, cancellationToken);
        if (inventoryResult.IsFailure) return Result.Failure(inventoryResult.Error);

        var inventory = inventoryResult.Value;
        inventory.ReservedQuantity += quantity;

        logger.LogInformation(
            "Reserved {Quantity} unit(s) for BranchId: {BranchId}, DrugId: {DrugId}. New ReservedQuantity: {ReservedQuantity} (Pending Save)",
            quantity, branchId, drugId, inventory.ReservedQuantity);

        return Result.Success();
    }

    public async Task<Result> ReleaseReservationAsync(Guid branchId, Guid drugId, int quantity,
        CancellationToken cancellationToken = default)
    {
        var inventoryResult = await FindAndValidateInventoryAsync(branchId, drugId, quantity, isRelease: true, cancellationToken);
        if (inventoryResult.IsFailure) return Result.Failure(inventoryResult.Error);

        var inventory = inventoryResult.Value;
        inventory.ReservedQuantity -= quantity;

        logger.LogInformation(
            "Released {Quantity} unit(s) for BranchId: {BranchId}, DrugId: {DrugId}. New ReservedQuantity: {ReservedQuantity} (Pending Save)",
            quantity, branchId, drugId, inventory.ReservedQuantity);

        return Result.Success();
    }

    public async Task<Result> ReserveStockBatchAsync(
        IEnumerable<(Guid BranchId, Guid DrugId, int Quantity)> reservations,
        CancellationToken cancellationToken = default)
    {
        var reservationsList = reservations.ToList();
        if (!reservationsList.Any()) return Result.Success();

        var inventoriesResult = await FindAndValidateBatchAsync(reservationsList, isRelease: false, cancellationToken);
        if (inventoriesResult.IsFailure) return Result.Failure(inventoriesResult.Error);

        var inventoryDict = inventoriesResult.Value;

        foreach (var res in reservationsList)
        {
            var inventory = inventoryDict[(res.BranchId, res.DrugId)];
            inventory.ReservedQuantity += res.Quantity;
        }

        logger.LogInformation("Successfully reserved {Count} batched items (Pending Save).", reservationsList.Count);
        return Result.Success();
    }

    public Result ReserveStockBatch(
        IEnumerable<PharmacyInventory> inventories,
        IEnumerable<(Guid BranchId, Guid DrugId, int Quantity)> reservations)
    {
        var reservationsList = reservations.ToList();
        if (!reservationsList.Any()) return Result.Success();

        var inventoryDict = inventories.ToDictionary(i => (i.BranchId, i.DrugId));

        foreach (var req in reservationsList)
        {
            if (!inventoryDict.TryGetValue((req.BranchId, req.DrugId), out var inventory))
            {
                logger.LogWarning("Inventory record not found for BranchId: {BranchId}, DrugId: {DrugId}", req.BranchId, req.DrugId);
                return Result.Failure(InventoryErrors.InventoryNotFound);
            }

            var validationResult = ValidateInventoryQuantity(inventory, req.BranchId, req.DrugId, req.Quantity, isRelease: false);
            if (validationResult.IsFailure)
                return Result.Failure(validationResult.Error);

            inventory.ReservedQuantity += req.Quantity;
        }

        logger.LogInformation("Successfully reserved {Count} batched items in-memory (Pending Save).", reservationsList.Count);
        return Result.Success();
    }

    public async Task<Result> ReleaseReservationBatchAsync(
        IEnumerable<(Guid BranchId, Guid DrugId, int Quantity)> releases,
        CancellationToken cancellationToken = default)
    {
        var releasesList = releases.ToList();
        if (!releasesList.Any()) return Result.Success();

        var inventoriesResult = await FindAndValidateBatchAsync(releasesList, isRelease: true, cancellationToken);
        if (inventoriesResult.IsFailure) return Result.Failure(inventoriesResult.Error);

        var inventoryDict = inventoriesResult.Value;

        foreach (var rel in releasesList)
        {
            var inventory = inventoryDict[(rel.BranchId, rel.DrugId)];
            inventory.ReservedQuantity -= rel.Quantity;
        }

        logger.LogInformation("Successfully released {Count} batched items (Pending Save).", releasesList.Count);
        return Result.Success();
    }

    private async Task<Result<PharmacyInventory>> FindAndValidateInventoryAsync(Guid branchId, Guid drugId, int quantity, bool isRelease, CancellationToken cancellationToken)
    {
        if (quantity <= 0)
            return Result.Failure<PharmacyInventory>(InventoryErrors.InvalidQuantity);

        if (branchId == Guid.Empty || drugId == Guid.Empty)
            return Result.Failure<PharmacyInventory>(InventoryErrors.InvalidIdentifier);

        var inventory = await context.PharmacyInventories
            .FirstOrDefaultAsync(i => i.BranchId == branchId && i.DrugId == drugId, cancellationToken);

        if (inventory is null)
        {
            logger.LogWarning("Inventory record not found for BranchId: {BranchId}, DrugId: {DrugId}", branchId, drugId);
            return Result.Failure<PharmacyInventory>(InventoryErrors.InventoryNotFound);
        }

        return ValidateInventoryQuantity(inventory, branchId, drugId, quantity, isRelease);
    }

    private async Task<Result<Dictionary<(Guid, Guid), PharmacyInventory>>> FindAndValidateBatchAsync(
        List<(Guid BranchId, Guid DrugId, int Quantity)> requests, bool isRelease, CancellationToken cancellationToken)
    {
        if (requests.Any(r => r.Quantity <= 0))
            return Result.Failure<Dictionary<(Guid, Guid), PharmacyInventory>>(InventoryErrors.InvalidQuantity);

        if (requests.Any(r => r.BranchId == Guid.Empty || r.DrugId == Guid.Empty))
            return Result.Failure<Dictionary<(Guid, Guid), PharmacyInventory>>(InventoryErrors.InvalidIdentifier);

        var branchIds = requests.Select(r => r.BranchId).Distinct().ToList();
        var drugIds = requests.Select(r => r.DrugId).Distinct().ToList();

        var inventories = await context.PharmacyInventories
            .Where(i => branchIds.Contains(i.BranchId) && drugIds.Contains(i.DrugId))
            .ToListAsync(cancellationToken);

        var inventoryDict = inventories.ToDictionary(i => (i.BranchId, i.DrugId));

        foreach (var req in requests)
        {
            if (!inventoryDict.TryGetValue((req.BranchId, req.DrugId), out var inventory))
            {
                logger.LogWarning("Inventory record not found for BranchId: {BranchId}, DrugId: {DrugId}", req.BranchId, req.DrugId);
                return Result.Failure<Dictionary<(Guid, Guid), PharmacyInventory>>(InventoryErrors.InventoryNotFound);
            }

            var validationResult = ValidateInventoryQuantity(inventory, req.BranchId, req.DrugId, req.Quantity, isRelease);
            if (validationResult.IsFailure)
                return Result.Failure<Dictionary<(Guid, Guid), PharmacyInventory>>(validationResult.Error);
        }

        return Result.Success(inventoryDict);
    }

    private Result<PharmacyInventory> ValidateInventoryQuantity(PharmacyInventory inventory, Guid branchId, Guid drugId, int quantity, bool isRelease)
    {
        if (isRelease)
        {
            if (quantity > inventory.ReservedQuantity)
            {
                logger.LogWarning("Release rejected for BranchId: {BranchId}, DrugId: {DrugId}. Requested: {Requested}, Currently reserved: {Reserved}",
                    branchId, drugId, quantity, inventory.ReservedQuantity);
                return Result.Failure<PharmacyInventory>(InventoryErrors.ReleaseExceedsReserved);
            }
        }
        else
        {
            var availableQuantity = inventory.StockQuantity - inventory.ReservedQuantity;
            if (availableQuantity < 0)
            {
                logger.LogError("Data integrity violation: ReservedQuantity ({Reserved}) exceeds StockQuantity ({Stock}) for BranchId: {BranchId}, DrugId: {DrugId}",
                    inventory.ReservedQuantity, inventory.StockQuantity, branchId, drugId);
                return Result.Failure<PharmacyInventory>(InventoryErrors.InsufficientStock);
            }

            if (availableQuantity < quantity)
            {
                logger.LogWarning("Insufficient stock for BranchId: {BranchId}, DrugId: {DrugId}. Available: {Available}, Requested: {Requested}",
                    branchId, drugId, availableQuantity, quantity);
                return Result.Failure<PharmacyInventory>(InventoryErrors.InsufficientStock);
            }
        }

        return Result.Success(inventory);
    }

    public async Task<Result<GetPharmacyInventoryDTO>> CreateAsync(AddPharmacyInventoryDto dto,
        CancellationToken cancellationToken = default)
    {
        var user = httpContextAccessor.HttpContext?.User;

        var branchAccess = EnsureBranchAccess(user, dto.BranchId);
        if (branchAccess.IsFailure)
            return Result.Failure<GetPharmacyInventoryDTO>(branchAccess.Error);

        var drugExists = await context.Drugs
            .AnyAsync(d => d.DrugId == dto.DrugId, cancellationToken);

        if (!drugExists)
            return Result.Failure<GetPharmacyInventoryDTO>(InventoryErrors.DrugNotFound);

        var alreadyExists = await context.PharmacyInventories
            .AnyAsync(i => i.BranchId == dto.BranchId && i.DrugId == dto.DrugId,
            cancellationToken);

        if (alreadyExists)
            return Result.Failure<GetPharmacyInventoryDTO>(InventoryErrors.AlreadyExist);

        var inventory = dto.Adapt<PharmacyInventory>();

        context.PharmacyInventories.Add(inventory);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created inventory {InventoryId} for Branch {BranchId}, Drug {DrugId}",
            inventory.InventoryId, inventory.BranchId, inventory.DrugId);

        var inventoryDto = inventory.Adapt<GetPharmacyInventoryDTO>();

        return Result.Success(inventoryDto);
    }

    public async Task<Result<GetPharmacyInventoryDTO>> UpdateAsync(Guid inventoryId, UpdatePharmacyInventoryDto dto,
        CancellationToken cancellationToken = default)
    {
        var user = httpContextAccessor.HttpContext?.User;

        var inventory = await context.PharmacyInventories
            .FirstOrDefaultAsync(i => i.InventoryId == inventoryId, cancellationToken);

        if (inventory is null)
            return Result.Failure<GetPharmacyInventoryDTO>(InventoryErrors.InventoryNotFound);

        var branchAccess = EnsureBranchAccess(user, inventory.BranchId);
        if (branchAccess.IsFailure)
            return Result.Failure<GetPharmacyInventoryDTO>(branchAccess.Error);

        if (dto.StockQuantity < inventory.ReservedQuantity)
        {
            logger.LogWarning(
                "Cannot update stock to {NewStock}. There are {Reserved} units already reserved for InventoryId: {InventoryId}",
                dto.StockQuantity, inventory.ReservedQuantity, inventoryId);

            return Result.Failure<GetPharmacyInventoryDTO>(InventoryErrors.StockLowerThanReserved);
        }

        inventory = dto.Adapt(inventory);

        if (dto.RowVersion is { Length: > 0 })
            context.Entry(inventory).Property(i => i.RowVersion).OriginalValue = dto.RowVersion;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("Concurrency conflict while updating InventoryId: {InventoryId}", inventoryId);
            return Result.Failure<GetPharmacyInventoryDTO>(InventoryErrors.ConcurrencyConflict);
        }

        var inventoryDto = inventory.Adapt<GetPharmacyInventoryDTO>();

        return Result.Success(inventoryDto);
    }

    public async Task<Result> DeleteAsync(Guid inventoryId, CancellationToken cancellationToken = default)
    {
        var user = httpContextAccessor.HttpContext?.User;

        var inventory = await context.PharmacyInventories
            .FirstOrDefaultAsync(i => i.InventoryId == inventoryId, cancellationToken);

        if (inventory is null)
            return Result.Failure(InventoryErrors.InventoryNotFound);

        var branchAccess = EnsureBranchAccess(user, inventory.BranchId);
        if (branchAccess.IsFailure)
            return branchAccess;

        if (inventory.ReservedQuantity > 0)
        {
            logger.LogWarning(
                "Delete rejected for InventoryId: {InventoryId}. {Reserved} unit(s) are still reserved.",
                inventoryId, inventory.ReservedQuantity);

            return Result.Failure(InventoryErrors.HasReservedStock);
        }

        context.PharmacyInventories.Remove(inventory);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Deleted inventory {InventoryId} from Branch {BranchId}", inventoryId, inventory.BranchId);

        return Result.Success();
    }

    public async Task<Result<PharmacyInventoryDto>> GetByIdAsync(Guid inventoryId,
        CancellationToken cancellationToken = default)
    {
        var user = httpContextAccessor.HttpContext?.User;
        var isAdmin = user?.IsInRole(AppRoles.Admin) ?? false;

        var query = context.PharmacyInventories.AsNoTracking()
            .Where(i => i.InventoryId == inventoryId);

        if (!isAdmin)
        {
            var branchIds = GetUserBranchIds(user);
            query = query.Where(i => branchIds.Contains(i.BranchId));
        }

        var dto = await query
            .ProjectToType<PharmacyInventoryDto>()
            .FirstOrDefaultAsync(cancellationToken);

        if (dto is null)
            return Result.Failure<PharmacyInventoryDto>(InventoryErrors.InventoryNotFound);

        dto.StockStatusLabel = ToStatusLabel(dto.StockStatus);

        return Result.Success(dto);
    }

    public async Task<Result<PaginatedList<GetPharmacyInventoryDTO>>> GetInventoryAsync(
        GetPharmacyInventoryParamRequest parameters, CancellationToken cancellationToken = default)
    {
        var user = httpContextAccessor.HttpContext?.User;

        var isAdmin = user?.IsInRole(AppRoles.Admin) ?? false;

        var query = context.PharmacyInventories.AsNoTracking();

        if (!isAdmin)
        {
            var branchIds = GetUserBranchIds(user);

            if (branchIds.Count == 0)
            {
                return Result.Success(new PaginatedList<GetPharmacyInventoryDTO>(
                    [],
                    parameters.PageNumber,
                    0,
                    parameters.PageSize
                ));
            }

            query = query.Where(i => branchIds.Contains(i.BranchId));
        }
        else
        {
            var adminId = user?.FindFirst(JwtClaimTypes.UserId)?.Value;
            logger.LogInformation("System Admin {AdminId} accessed global inventory records at {Time}", adminId,
                DateTime.UtcNow);
        }

        if (parameters.BranchId is { } branchId && branchId != Guid.Empty)
            query = query.Where(i => i.BranchId == branchId);

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var term = parameters.Search.Trim();

            query = query.Where(i =>
                EF.Functions.Like(i.Drug.BrandName, $"%{term}%") ||
                EF.Functions.Like(i.Drug.GenericName, $"%{term}%"));
        }

        query = parameters.StatusFilter switch
        {
            InventoryStatusFilter.Available => query.Where(i => i.StockQuantity > LowStockThreshold),
            InventoryStatusFilter.LowStock => query.Where(i => i.StockQuantity <= LowStockThreshold && i.StockQuantity > 0),
            InventoryStatusFilter.OutOfStock => query.Where(i => i.StockQuantity == 0),
            _ => query
        };

        var page = await query
            .OrderBy(i => i.LastSyncedAt)
            .ProjectToType<GetPharmacyInventoryDTO>()
            .ToPaginatedListAsync(parameters.PageNumber, parameters.PageSize, cancellationToken);

        foreach (var item in page.Items)
            item.StockStatusLabel = ToStatusLabel(item.StockStatus);

        return Result.Success(page);
    }

    private static string ToStatusLabel(InventoryStockStatus status) => status switch
    {
        InventoryStockStatus.Available => "Available",
        InventoryStockStatus.LowStock => "Low Stock",
        InventoryStockStatus.OutOfStock => "Out of Stock",
        _ => status.ToString()
    };

    private static List<Guid> GetUserBranchIds(ClaimsPrincipal? user) =>
        user?.FindAll(JwtClaimTypes.BranchId)
            .Select(c => Guid.TryParse(c.Value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList() ?? [];

    private Result EnsureBranchAccess(ClaimsPrincipal? user, Guid branchId)
    {
        if (user?.IsInRole(AppRoles.Admin) ?? false)
            return Result.Success();

        var branchIds = GetUserBranchIds(user);

        if (!branchIds.Contains(branchId))
        {
            var userId = user?.FindFirst(JwtClaimTypes.UserId)?.Value;
            logger.LogWarning("User {UserId} attempted to manage inventory for unauthorized Branch {BranchId}",
                userId, branchId);

            return Result.Failure(InventoryErrors.DifferentBranch);
        }

        return Result.Success();
    }
}


