namespace Application.Services.FulfillmentLeg;

public interface ILegGenerationService
{
    Result<List<OrderFulfillmentLeg>> GenerateLegs(Domain.Entities.Order order, IEnumerable<Guid> assignedBranchIds);
}