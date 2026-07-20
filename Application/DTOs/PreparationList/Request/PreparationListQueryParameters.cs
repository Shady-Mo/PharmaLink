using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.PreparationList.Request
{
    public class PreparationListQueryParameters
    {
        public string? SearchTerm { get; set; }
        public LegStatus? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
