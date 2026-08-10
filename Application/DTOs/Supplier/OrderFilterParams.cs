using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Supplier
{
    public class OrderFilterParams
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public POStatus? Status { get; set; }
    }
}
