using Application.Services.FulfillmentLeg;
using Application.Settings;
using Domain.Entities;
using Domain.Enums;
using Application.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class LegGenerationService(
    IOptions<OrderFulfillmentSettings> options,
    ILogger<LegGenerationService> logger) : ILegGenerationService
{
    private readonly OrderFulfillmentSettings _settings = options.Value;

    public Result<List<OrderFulfillmentLeg>> GenerateLegs(
        Domain.Entities.Order order,
        IEnumerable<Guid> assignedBranchIds,
        IReadOnlyDictionary<Guid, double>? distanceByBranchKm = null)
    {

        var distinctBranchIds = assignedBranchIds.Distinct().ToList();

        if (distinctBranchIds.Count == 0)
        {
            logger.LogWarning("[OrderId={OrderId}] No branches assigned. Skipping leg generation.", order.OrderId);
            return Result.Success(new List<OrderFulfillmentLeg>());
        }

        var calculatedLegType = order.FulfillmentMode == FulfillmentMode.Delivery
            ? LegType.Delivery
            : LegType.Preparation;

        var readyByEstimateTime = DateTime.UtcNow.AddMinutes(_settings.EstimatedPreparationMinutes);

        var legs = new List<OrderFulfillmentLeg>();

        foreach (var branchId in distinctBranchIds)
        {
            double? distanceKm = null;
            if (distanceByBranchKm is not null
                && distanceByBranchKm.TryGetValue(branchId, out var km)
                && km < double.MaxValue)
            {
                distanceKm = Math.Round(km, 3);
            }

            legs.Add(new OrderFulfillmentLeg
            {
                LegId = Guid.NewGuid(),
                OrderId = order.OrderId,
                BranchId = branchId,
                LegStatus = LegStatus.Assigned,
                LegType = calculatedLegType,
                ReadyByEstimate = readyByEstimateTime,
                DistanceKm = distanceKm
            });

        }

        return Result.Success(legs);
    }
}
