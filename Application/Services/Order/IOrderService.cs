namespace Application.Services.Order
{
    public interface IOrderService
    {
        public Task<Result<OrderCreatedResponseDTO>> CreateOrder(Guid patientUserId, CreateOrderDTO createOrderDTO, IFulfillmentEngineService _fulfillmentEngineService);
        public Task<Result<GetOrderDTO>> GetOrder(Guid orderId, Guid patientUserId);
        public Task<Result<PaginatedList<GetOrderDTO>>> GetOrders(Guid patientUserId, int pageNumber = 1, int pageSize = 10);
    }
}
