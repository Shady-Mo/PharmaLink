using Application.DTOs.PharmacyInventory.Request;
using Application.DTOs.PharmacyInventory.Response;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services;

public class InventoryService(AppDbContext context, ILogger<InventoryService> logger, IHttpContextAccessor httpContextAccessor) : IInventoryService
{
    public async Task<Result> ReserveStockAsync(Guid branchId, Guid drugId, int quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            return Result.Failure(InventoryErrors.InvalidQuantity);

        if (branchId == Guid.Empty || drugId == Guid.Empty)
            return Result.Failure(InventoryErrors.InvalidIdentifier);

        var inventory = await FindInventoryAsync(branchId, drugId, cancellationToken);

        if (inventory is null)
        {
            logger.LogWarning(
                "Inventory record not found for BranchId: {BranchId}, DrugId: {DrugId}",
                branchId, drugId);

            return Result.Failure(InventoryErrors.InventoryNotFound);
        }

        var availableQuantity = inventory.StockQuantity - inventory.ReservedQuantity;

        if (availableQuantity < 0)
        {
            logger.LogError(
                "Data integrity violation: ReservedQuantity ({Reserved}) exceeds StockQuantity ({Stock}) " +
                "for BranchId: {BranchId}, DrugId: {DrugId}",
                inventory.ReservedQuantity, inventory.StockQuantity, branchId, drugId);

            return Result.Failure(InventoryErrors.InsufficientStock);
        }

        if (availableQuantity < quantity)
        {
            logger.LogWarning(
                "Insufficient stock for BranchId: {BranchId}, DrugId: {DrugId}. " +
                "Available: {Available}, Requested: {Requested}",
                branchId, drugId, availableQuantity, quantity);

            return Result.Failure(InventoryErrors.InsufficientStock);
        }

        inventory.ReservedQuantity += quantity;

        try
        {
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Reserved {Quantity} unit(s) for BranchId: {BranchId}, DrugId: {DrugId}. " +
                "New ReservedQuantity: {ReservedQuantity}",
                quantity, branchId, drugId, inventory.ReservedQuantity);

            return Result.Success();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex,
                "Concurrency conflict while reserving stock for BranchId: {BranchId}, DrugId: {DrugId}",
                branchId, drugId);

            return Result.Failure(InventoryErrors.ConcurrencyConflict);
        }
    }

    public async Task<Result> ReleaseReservationAsync(Guid branchId, Guid drugId, int quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            return Result.Failure(InventoryErrors.InvalidQuantity);

        if (branchId == Guid.Empty || drugId == Guid.Empty)
            return Result.Failure(InventoryErrors.InvalidIdentifier);

        var inventory = await FindInventoryAsync(branchId, drugId, cancellationToken);

        if (inventory is null)
        {
            logger.LogWarning(
                "Inventory record not found for BranchId: {BranchId}, DrugId: {DrugId}",
                branchId, drugId);

            return Result.Failure(InventoryErrors.InventoryNotFound);
        }

        if (quantity > inventory.ReservedQuantity)
        {
            logger.LogWarning(
                "Release rejected for BranchId: {BranchId}, DrugId: {DrugId}. " +
                "Requested release: {Requested}, Currently reserved: {Reserved}",
                branchId, drugId, quantity, inventory.ReservedQuantity);

            return Result.Failure(InventoryErrors.ReleaseExceedsReserved);
        }

        inventory.ReservedQuantity -= quantity;

        try
        {
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Released {Quantity} unit(s) for BranchId: {BranchId}, DrugId: {DrugId}. " +
                "New ReservedQuantity: {ReservedQuantity}",
                quantity, branchId, drugId, inventory.ReservedQuantity);

            return Result.Success();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex,
                "Concurrency conflict while releasing stock for BranchId: {BranchId}, DrugId: {DrugId}",
                branchId, drugId);

            return Result.Failure(InventoryErrors.ConcurrencyConflict);
        }
    }

    private Task<PharmacyInventory?>
        FindInventoryAsync(Guid branchId, Guid drugId, CancellationToken cancellationToken) =>
        context.PharmacyInventories.FirstOrDefaultAsync(i => i.BranchId == branchId && i.DrugId == drugId,
            cancellationToken);

    public async Task<Result<PharmacyInventoryDto>> CreateAsync(AddPharmacyInventoryDto dto, CancellationToken cancellationToken = default)
    {
        var isFound = context.PharmacyInventories.Where(i => i.BranchId == dto.BranchId && i.DrugId == dto.DrugId).Any();

        if(isFound)
            return Result.Failure<PharmacyInventoryDto>(InventoryErrors.AlreadyExist);


        var user = httpContextAccessor.HttpContext?.User;


        var branchIds = user.FindAll(JwtClaimTypes.BranchId)
                                      .Select(c => Guid.Parse(c.Value))
                                      .ToList();

        if (!branchIds.Contains(dto.BranchId))
        {
            var userId = user.FindFirst(JwtClaimTypes.UserId)?.Value;
            logger.LogWarning("User {UserId} attempted to add inventory to unauthorized Branch {BranchId}", userId, dto.BranchId);

            return Result.Failure<PharmacyInventoryDto>(InventoryErrors.DifferentBranch);
        }

        var pharmacyInventory = dto.Adapt<PharmacyInventory>();

        pharmacyInventory.InventoryId = Guid.NewGuid();


        context.PharmacyInventories.Add(pharmacyInventory);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(pharmacyInventory.Adapt<PharmacyInventoryDto>());
    }


    public async Task<Result<PharmacyInventoryDto>> UpdateAsync(UpdatePharmacyInventoryDto dto, CancellationToken cancellationToken = default)
    {
        var user = httpContextAccessor.HttpContext?.User;

        var branchIds = user?.FindAll(JwtClaimTypes.BranchId)
                            .Select(c => Guid.Parse(c.Value))
                            .ToList() ?? new List<Guid>();

        if (!branchIds.Contains(dto.BranchId))
        {
            var userId = user?.FindFirst(JwtClaimTypes.UserId)?.Value;
            logger.LogWarning("User {UserId} attempted to update inventory in unauthorized Branch {BranchId}", userId, dto.BranchId);

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

    public async Task<Result<PaginatedList<GetPharmacyInventoryDTO>>> GetInventoryAsync(GetPharmacyInventoryParamRequest parameters, CancellationToken cancellationToken = default)
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
            logger.LogInformation("System Admin {AdminId} accessed global inventory records at {Time}", adminId, DateTime.UtcNow);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(i => i.Drug)
            .OrderBy(i => i.InventoryId)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        var result = new PaginatedList<GetPharmacyInventoryDTO>(
            items.Adapt<List<GetPharmacyInventoryDTO>>(),
            parameters.PageNumber,
            totalCount,
            parameters.PageSize
        );

        return Result.Success(result);
    }
}