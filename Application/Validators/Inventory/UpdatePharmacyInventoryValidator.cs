using Application.DTOs.PharmacyInventory.Request;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Validators.Inventory
{
    public class UpdatePharmacyInventoryValidator : AbstractValidator<AddPharmacyInventoryDto>
    {
        public UpdatePharmacyInventoryValidator()
        {
            RuleFor(i => i.BranchId).NotEmpty();
            RuleFor(i => i.DrugId).NotEmpty();
            RuleFor(i => i.StockQuantity).GreaterThan(0);
            RuleFor(i => i.UnitPrice).GreaterThan(0);
        }
    }
}
