using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Supplier
{
    public class UpdateOrderStatusDto
    {
        public POStatus NewStatus { get; set; }
    }
}
