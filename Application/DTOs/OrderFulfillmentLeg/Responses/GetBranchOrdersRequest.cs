using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.OrderFulfillmentLeg.Responses
{
    public class GetBranchOrdersRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public LegStatus? Status { get; set; }
    }
}
