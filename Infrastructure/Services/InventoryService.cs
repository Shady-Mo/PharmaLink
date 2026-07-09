namespace Infrastructure.Services;

public class InventoryService(AppDbContext context, ILogger<InventoryService> logger) : IInventoryService
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
}