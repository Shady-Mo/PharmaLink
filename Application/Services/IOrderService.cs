namespace Application.Services.Order
{
    public interface IOrderService
    {
        Task<Result<OrderCreatedResponseDTO>> CreateOrder(Guid patientUserId, CreateOrderDTO createOrderDTO);
        Task<Result<GetOrderDTO>> GetOrder(Guid orderId, Guid patientUserId);
        Task<Result<PaginatedList<GetOrderDTO>>> GetOrders(Guid patientUserId, GetOrdersRequest request);
        Task<Result<string>> CancelOrder(Guid orderId, Guid patientUserId, CancellationToken ct = default);

        // ── Admin-only ──────────────────────────────────────────────────────
        Task<Result<GetOrderDTO>> GetOrderForAdmin(Guid orderId);
        Task<Result<PaginatedList<GetOrderDTO>>> GetOrdersForAdmin(GetOrdersRequest request);

        /// <summary>Returns a flat admin order list with search/filter/sort applied.</summary>
        Task<Result<PaginatedList<AdminOrderDTO>>> GetAdminOrders(GetOrdersRequest request, CancellationToken ct = default);

        /// <summary>Returns the full order detail for the admin detail page.</summary>
        Task<Result<AdminOrderDetailDTO>> GetAdminOrderDetail(Guid orderId, CancellationToken ct = default);

        Task<Result<string>> ApproveOrderPrescription(Guid orderId, CancellationToken ct = default);
        Task<Result<string>> RejectOrderPrescription(Guid orderId, string reason, CancellationToken ct = default);

        /// <summary>Removes a specific drug item from an order (only allowed before fulfillment).</summary>
        Task<Result<string>> RemoveOrderItemAsync(Guid orderId, Guid orderItemId, CancellationToken ct = default);

        /// <summary>Exports filtered orders as xlsx or csv bytes.</summary>
        Task<Result<(byte[] Data, string ContentType, string FileName)>> ExportOrdersForAdmin(
            ExportOrdersRequest request, CancellationToken ct = default);
    }
}
