namespace Application.DTOs.Order.Responses
{
    public class OrderCreatedResponseDTO
    {
        public Guid OrderId { get; set; }
        public OrderStatus Status { get; set; }
        public string Message { get; set; } = default!;

        /// <summary>URL to redirect the user for payment if Stripe is selected.</summary>
        public string? PaymentUrl { get; set; }

        /// <summary>Optimization strategy used by the fulfillment engine (e.g. "AI-MultiAgent").</summary>
        public string Strategy { get; set; } = string.Empty;

        /// <summary>True when every requested item was allocated to a fulfilling branch.</summary>
        public bool IsFullyFulfilled { get; set; }

        /// <summary>Total driving distance (km) across all pickup legs.</summary>
        public double TotalDistanceKm { get; set; }

        /// <summary>
        /// Each fulfilling pharmacy branch, its distance from the patient, and the group of drugs
        /// (Arabic + English names) that branch supplies — "كل مجموعة أدوية موجودين في صيدلية على بعد كام".
        /// </summary>
        public IReadOnlyList<OrderFulfillmentGroupDTO> FulfillmentGroups { get; set; } = [];

        /// <summary>Requested items that no branch could supply (Arabic + English names).</summary>
        public IReadOnlyList<UnavailableItemDTO> UnavailableItems { get; set; } = [];
    }

    /// <summary>
    /// A group of drugs available together at one pharmacy branch, with the branch's distance from
    /// the patient. This is one pickup leg of the fulfillment plan.
    /// </summary>
    public class OrderFulfillmentGroupDTO
    {
        public Guid PharmacyId { get; set; }
        public Guid BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;

        /// <summary>Real-world driving distance (km) from the patient to this branch.</summary>
        public double DistanceKm { get; set; }

        /// <summary>Sum of the line totals supplied by this branch.</summary>
        public decimal Subtotal { get; set; }

        public IReadOnlyList<OrderItemLineDTO> Items { get; set; } = [];
    }

    /// <summary>An available (fulfilled) line item carrying both English and Arabic drug names.</summary>
    public class OrderItemLineDTO
    {
        public Guid DrugId { get; set; }

        /// <summary>English brand name.</summary>
        public string DrugName { get; set; } = string.Empty;

        /// <summary>Arabic name.</summary>
        public string DrugNameAr { get; set; } = string.Empty;

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    /// <summary>A requested item that could not be fulfilled, with English and Arabic drug names.</summary>
    public class UnavailableItemDTO
    {
        public Guid DrugId { get; set; }

        /// <summary>English brand name.</summary>
        public string DrugName { get; set; } = string.Empty;

        /// <summary>Arabic name.</summary>
        public string DrugNameAr { get; set; } = string.Empty;

        public int QuantityNeeded { get; set; }
        public int QuantityAvailable { get; set; }
    }
}
