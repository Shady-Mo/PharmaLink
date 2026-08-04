using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class ForecastLogDto
    {
        public Guid DrugId { get; set; }
        public DateTime ForecastDate { get; set; }
        public DateTime? PredictedStockoutDate { get; set; }
        public string AiRationale { get; set; } = string.Empty;
        public string DrugName { get; set; }
    }
}
