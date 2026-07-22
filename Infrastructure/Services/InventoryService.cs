namespace Infrastructure.Services;

public class InventoryService(
    AppDbContext context,
    ILogger<InventoryService> logger,
    IHttpContextAccessor httpContextAccessor) : IInventoryService
{
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

    public async Task<Result<PharmacyInventoryDto>> CreateAsync(AddPharmacyInventoryDto dto,
        CancellationToken cancellationToken = default)
    {
        var isFound = await context.PharmacyInventories.AnyAsync(
            i => i.BranchId == dto.BranchId &&
                 i.DrugId == dto.DrugId,
            cancellationToken);

        if (isFound)
            return Result.Failure<PharmacyInventoryDto>(InventoryErrors.AlreadyExist);


        var user = httpContextAccessor.HttpContext?.User;


        var branchIds = user.FindAll(JwtClaimTypes.BranchId)
            .Select(c => Guid.Parse(c.Value))
            .ToList();

        if (!branchIds.Contains(dto.BranchId))
        {
            var userId = user.FindFirst(JwtClaimTypes.UserId)?.Value;
            logger.LogWarning("User {UserId} attempted to add inventory to unauthorized Branch {BranchId}", userId,
                dto.BranchId);

            return Result.Failure<PharmacyInventoryDto>(InventoryErrors.DifferentBranch);
        }

        var pharmacyInventory = dto.Adapt<PharmacyInventory>();

        pharmacyInventory.InventoryId = Guid.NewGuid();


        context.PharmacyInventories.Add(pharmacyInventory);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(pharmacyInventory.Adapt<PharmacyInventoryDto>());
    }


    public async Task<Result<PharmacyInventoryDto>> UpdateAsync(UpdatePharmacyInventoryDto dto,
        CancellationToken cancellationToken = default)
    {
        var user = httpContextAccessor.HttpContext?.User;

        var branchIds = user?.FindAll(JwtClaimTypes.BranchId)
            .Select(c => Guid.Parse(c.Value))
            .ToList() ?? new List<Guid>();

        if (!branchIds.Contains(dto.BranchId))
        {
            var userId = user?.FindFirst(JwtClaimTypes.UserId)?.Value;
            logger.LogWarning("User {UserId} attempted to update inventory in unauthorized Branch {BranchId}", userId,
                dto.BranchId);

            return Result.Failure<PharmacyInventoryDto>(InventoryErrors.DifferentBranch);
        }

        var inventory = await context.PharmacyInventories
            .FirstOrDefaultAsync(i => i.BranchId == dto.BranchId && i.DrugId == dto.DrugId, cancellationToken);

        if (inventory is null)
            return Result.Failure<PharmacyInventoryDto>(InventoryErrors.InventoryNotFound);

        if (dto.StockQuantity < inventory.ReservedQuantity)
        {
            logger.LogWarning(
                "Cannot update stock to {NewStock}. There are {Reserved} units already reserved for BranchId: {BranchId}, DrugId: {DrugId}",
                dto.StockQuantity, inventory.ReservedQuantity, dto.BranchId, dto.DrugId);

            return Result.Failure<PharmacyInventoryDto>(InventoryErrors.StockLowerThanReserved);
        }

        inventory.StockQuantity = dto.StockQuantity;
        inventory.UnitPrice = dto.UnitPrice;
        inventory.ExpiryDate = dto.ExpiryDate;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(inventory.Adapt<PharmacyInventoryDto>());
    }

    public async Task<Result<PaginatedList<GetPharmacyInventoryDTO>>> GetInventoryAsync(
        GetPharmacyInventoryParamRequest parameters, CancellationToken cancellationToken = default)
    {
        var user = httpContextAccessor.HttpContext?.User;

        var isAdmin = user.IsInRole(AppRoles.Admin);

        var query = context.PharmacyInventories.AsNoTracking().AsQueryable();

        if (!isAdmin)
        {
            var branchIds = user.FindAll(JwtClaimTypes.BranchId)
                .Select(c => Guid.Parse(c.Value))
                .ToList();

            if (!branchIds.Any())
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
            var adminId = user.FindFirst(JwtClaimTypes.UserId)?.Value;
            logger.LogInformation("System Admin {AdminId} accessed global inventory records at {Time}", adminId,
                DateTime.UtcNow);
        }

        if (parameters.Status == StockStatus.LowStock)
            query = query.Where(i => i.StockQuantity < 10);

        if (parameters.Status == StockStatus.ExpiredSoon)
        {
            var targetExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
            query = query.Where(i => i.ExpiryDate < targetExpiryDate);
        }

        if (!string.IsNullOrWhiteSpace(parameters.SerachByName))
        {
            var search = parameters.SerachByName.Trim();
            query = query.Where(i => (i.Drug != null && i.Drug.BrandName.Contains(search) || (i.Drug != null && i.Drug.GenericName.Contains(search)) || (i.Drug != null && i.Drug.ArabicName.Contains(search))));
        }

        var result = await query
            .OrderBy(i => i.InventoryId)
            .ProjectToType<GetPharmacyInventoryDTO>()
            .ToPaginatedListAsync(parameters.PageNumber, parameters.PageSize, cancellationToken);

        return Result.Success(result);
    }
}