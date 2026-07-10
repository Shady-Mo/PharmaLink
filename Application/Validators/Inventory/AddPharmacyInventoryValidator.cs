using Application.DTOs.PharmacyInventory.Request;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Validators.Inventory
{
    internal class AddPharmacyInventoryValidator: AbstractValidator<AddPharmacyInventoryDto>
    {
        public AddPharmacyInventoryValidator()
        {
            RuleFor(i => i.BranchId).NotEmpty();
            RuleFor(i => i.DrugId).NotEmpty();
            RuleFor(i => i.StockQuantity).GreaterThan(0);
            RuleFor(i => i.UnitPrice).GreaterThan(0);
        }
    }
}
