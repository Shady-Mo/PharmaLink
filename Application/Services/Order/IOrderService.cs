namespace Application.Services.Order
{
    public interface IOrderService
    {
        public Task<Result<OrderCreatedResponseDTO>> CreateOrder(Guid patientUserId, CreateOrderDTO createOrderDTO);
        public Task<Result<GetOrderDTO>> GetOrder(Guid orderId, Guid patientUserId);
        public Task<Result<PaginatedList<GetOrderDTO>>> GetOrders(Guid patientUserId, GetOrdersRequest request);
        public Task<Result<GetOrderDTO>> GetOrderForAdmin(Guid orderId);
        public Task<Result<PaginatedList<GetOrderDTO>>> GetOrdersForAdmin(GetOrdersRequest request);
    }
}
