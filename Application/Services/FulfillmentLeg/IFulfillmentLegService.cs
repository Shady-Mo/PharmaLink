namespace Application.Services.FulfillmentLeg
{
    public interface IFulfillmentLegService
    {
        Task<Result<bool>> GenerateLegsAsync(Guid orderId);
    }
}
