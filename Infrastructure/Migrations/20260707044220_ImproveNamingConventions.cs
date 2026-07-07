using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ImproveNamingConventions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_AspNetUsers_UserID",
                table: "Addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderFulfillmentLegs_Orders_OrderID",
                table: "OrderFulfillmentLegs");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderFulfillmentLegs_PharmacyBranches_BranchID",
                table: "OrderFulfillmentLegs");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Drugs_DrugID",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Orders_OrderID",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_PharmacyBranches_BranchID",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Addresses_DeliveryAddressID",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_AspNetUsers_PatientUserID",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Pharmacies_AspNetUsers_OwnerUserID",
                table: "Pharmacies");

            migrationBuilder.DropForeignKey(
                name: "FK_PharmacyBranches_Pharmacies_PharmacyID",
                table: "PharmacyBranches");

            migrationBuilder.DropForeignKey(
                name: "FK_PharmacyInventories_Drugs_DrugID",
                table: "PharmacyInventories");

            migrationBuilder.DropForeignKey(
                name: "FK_PharmacyInventories_PharmacyBranches_BranchID",
                table: "PharmacyInventories");

            migrationBuilder.RenameColumn(
                name: "DrugID",
                table: "PharmacyInventories",
                newName: "DrugId");

            migrationBuilder.RenameColumn(
                name: "BranchID",
                table: "PharmacyInventories",
                newName: "BranchId");

            migrationBuilder.RenameColumn(
                name: "InventoryID",
                table: "PharmacyInventories",
                newName: "InventoryId");

            migrationBuilder.RenameIndex(
                name: "IX_PharmacyInventories_DrugID",
                table: "PharmacyInventories",
                newName: "IX_PharmacyInventories_DrugId");

            migrationBuilder.RenameIndex(
                name: "IX_PharmacyInventories_BranchID",
                table: "PharmacyInventories",
                newName: "IX_PharmacyInventories_BranchId");

            migrationBuilder.RenameColumn(
                name: "PharmacyID",
                table: "PharmacyBranches",
                newName: "PharmacyId");

            migrationBuilder.RenameColumn(
                name: "BranchID",
                table: "PharmacyBranches",
                newName: "BranchId");

            migrationBuilder.RenameIndex(
                name: "IX_PharmacyBranches_PharmacyID",
                table: "PharmacyBranches",
                newName: "IX_PharmacyBranches_PharmacyId");

            migrationBuilder.RenameColumn(
                name: "OwnerUserID",
                table: "Pharmacies",
                newName: "OwnerUserId");

            migrationBuilder.RenameColumn(
                name: "PharmacyID",
                table: "Pharmacies",
                newName: "PharmacyId");

            migrationBuilder.RenameIndex(
                name: "IX_Pharmacies_OwnerUserID",
                table: "Pharmacies",
                newName: "IX_Pharmacies_OwnerUserId");

            migrationBuilder.RenameColumn(
                name: "PatientUserID",
                table: "Orders",
                newName: "PatientUserId");

            migrationBuilder.RenameColumn(
                name: "DeliveryAddressID",
                table: "Orders",
                newName: "DeliveryAddressId");

            migrationBuilder.RenameColumn(
                name: "OrderID",
                table: "Orders",
                newName: "OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_PatientUserID",
                table: "Orders",
                newName: "IX_Orders_PatientUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_DeliveryAddressID",
                table: "Orders",
                newName: "IX_Orders_DeliveryAddressId");

            migrationBuilder.RenameColumn(
                name: "OrderID",
                table: "OrderItems",
                newName: "OrderId");

            migrationBuilder.RenameColumn(
                name: "DrugID",
                table: "OrderItems",
                newName: "DrugId");

            migrationBuilder.RenameColumn(
                name: "BranchID",
                table: "OrderItems",
                newName: "BranchId");

            migrationBuilder.RenameColumn(
                name: "OrderItemID",
                table: "OrderItems",
                newName: "OrderItemId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItems_OrderID",
                table: "OrderItems",
                newName: "IX_OrderItems_OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItems_DrugID",
                table: "OrderItems",
                newName: "IX_OrderItems_DrugId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItems_BranchID",
                table: "OrderItems",
                newName: "IX_OrderItems_BranchId");

            migrationBuilder.RenameColumn(
                name: "OrderID",
                table: "OrderFulfillmentLegs",
                newName: "OrderId");

            migrationBuilder.RenameColumn(
                name: "BranchID",
                table: "OrderFulfillmentLegs",
                newName: "BranchId");

            migrationBuilder.RenameColumn(
                name: "LegID",
                table: "OrderFulfillmentLegs",
                newName: "LegId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderFulfillmentLegs_OrderID",
                table: "OrderFulfillmentLegs",
                newName: "IX_OrderFulfillmentLegs_OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderFulfillmentLegs_BranchID",
                table: "OrderFulfillmentLegs",
                newName: "IX_OrderFulfillmentLegs_BranchId");

            migrationBuilder.RenameColumn(
                name: "RxNormCUI",
                table: "Drugs",
                newName: "RxNormCui");

            migrationBuilder.RenameColumn(
                name: "DrugBankID",
                table: "Drugs",
                newName: "DrugBankId");

            migrationBuilder.RenameColumn(
                name: "DrugID",
                table: "Drugs",
                newName: "DrugId");

            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "Addresses",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "AddressID",
                table: "Addresses",
                newName: "AddressId");

            migrationBuilder.RenameIndex(
                name: "IX_Addresses_UserID",
                table: "Addresses",
                newName: "IX_Addresses_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Addresses_AspNetUsers_UserId",
                table: "Addresses",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderFulfillmentLegs_Orders_OrderId",
                table: "OrderFulfillmentLegs",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderFulfillmentLegs_PharmacyBranches_BranchId",
                table: "OrderFulfillmentLegs",
                column: "BranchId",
                principalTable: "PharmacyBranches",
                principalColumn: "BranchId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Drugs_DrugId",
                table: "OrderItems",
                column: "DrugId",
                principalTable: "Drugs",
                principalColumn: "DrugId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Orders_OrderId",
                table: "OrderItems",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_PharmacyBranches_BranchId",
                table: "OrderItems",
                column: "BranchId",
                principalTable: "PharmacyBranches",
                principalColumn: "BranchId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Addresses_DeliveryAddressId",
                table: "Orders",
                column: "DeliveryAddressId",
                principalTable: "Addresses",
                principalColumn: "AddressId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_AspNetUsers_PatientUserId",
                table: "Orders",
                column: "PatientUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pharmacies_AspNetUsers_OwnerUserId",
                table: "Pharmacies",
                column: "OwnerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PharmacyBranches_Pharmacies_PharmacyId",
                table: "PharmacyBranches",
                column: "PharmacyId",
                principalTable: "Pharmacies",
                principalColumn: "PharmacyId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PharmacyInventories_Drugs_DrugId",
                table: "PharmacyInventories",
                column: "DrugId",
                principalTable: "Drugs",
                principalColumn: "DrugId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PharmacyInventories_PharmacyBranches_BranchId",
                table: "PharmacyInventories",
                column: "BranchId",
                principalTable: "PharmacyBranches",
                principalColumn: "BranchId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_AspNetUsers_UserId",
                table: "Addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderFulfillmentLegs_Orders_OrderId",
                table: "OrderFulfillmentLegs");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderFulfillmentLegs_PharmacyBranches_BranchId",
                table: "OrderFulfillmentLegs");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Drugs_DrugId",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Orders_OrderId",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_PharmacyBranches_BranchId",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Addresses_DeliveryAddressId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_AspNetUsers_PatientUserId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Pharmacies_AspNetUsers_OwnerUserId",
                table: "Pharmacies");

            migrationBuilder.DropForeignKey(
                name: "FK_PharmacyBranches_Pharmacies_PharmacyId",
                table: "PharmacyBranches");

            migrationBuilder.DropForeignKey(
                name: "FK_PharmacyInventories_Drugs_DrugId",
                table: "PharmacyInventories");

            migrationBuilder.DropForeignKey(
                name: "FK_PharmacyInventories_PharmacyBranches_BranchId",
                table: "PharmacyInventories");

            migrationBuilder.RenameColumn(
                name: "DrugId",
                table: "PharmacyInventories",
                newName: "DrugID");

            migrationBuilder.RenameColumn(
                name: "BranchId",
                table: "PharmacyInventories",
                newName: "BranchID");

            migrationBuilder.RenameColumn(
                name: "InventoryId",
                table: "PharmacyInventories",
                newName: "InventoryID");

            migrationBuilder.RenameIndex(
                name: "IX_PharmacyInventories_DrugId",
                table: "PharmacyInventories",
                newName: "IX_PharmacyInventories_DrugID");

            migrationBuilder.RenameIndex(
                name: "IX_PharmacyInventories_BranchId",
                table: "PharmacyInventories",
                newName: "IX_PharmacyInventories_BranchID");

            migrationBuilder.RenameColumn(
                name: "PharmacyId",
                table: "PharmacyBranches",
                newName: "PharmacyID");

            migrationBuilder.RenameColumn(
                name: "BranchId",
                table: "PharmacyBranches",
                newName: "BranchID");

            migrationBuilder.RenameIndex(
                name: "IX_PharmacyBranches_PharmacyId",
                table: "PharmacyBranches",
                newName: "IX_PharmacyBranches_PharmacyID");

            migrationBuilder.RenameColumn(
                name: "OwnerUserId",
                table: "Pharmacies",
                newName: "OwnerUserID");

            migrationBuilder.RenameColumn(
                name: "PharmacyId",
                table: "Pharmacies",
                newName: "PharmacyID");

            migrationBuilder.RenameIndex(
                name: "IX_Pharmacies_OwnerUserId",
                table: "Pharmacies",
                newName: "IX_Pharmacies_OwnerUserID");

            migrationBuilder.RenameColumn(
                name: "PatientUserId",
                table: "Orders",
                newName: "PatientUserID");

            migrationBuilder.RenameColumn(
                name: "DeliveryAddressId",
                table: "Orders",
                newName: "DeliveryAddressID");

            migrationBuilder.RenameColumn(
                name: "OrderId",
                table: "Orders",
                newName: "OrderID");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_PatientUserId",
                table: "Orders",
                newName: "IX_Orders_PatientUserID");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_DeliveryAddressId",
                table: "Orders",
                newName: "IX_Orders_DeliveryAddressID");

            migrationBuilder.RenameColumn(
                name: "OrderId",
                table: "OrderItems",
                newName: "OrderID");

            migrationBuilder.RenameColumn(
                name: "DrugId",
                table: "OrderItems",
                newName: "DrugID");

            migrationBuilder.RenameColumn(
                name: "BranchId",
                table: "OrderItems",
                newName: "BranchID");

            migrationBuilder.RenameColumn(
                name: "OrderItemId",
                table: "OrderItems",
                newName: "OrderItemID");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                newName: "IX_OrderItems_OrderID");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItems_DrugId",
                table: "OrderItems",
                newName: "IX_OrderItems_DrugID");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItems_BranchId",
                table: "OrderItems",
                newName: "IX_OrderItems_BranchID");

            migrationBuilder.RenameColumn(
                name: "OrderId",
                table: "OrderFulfillmentLegs",
                newName: "OrderID");

            migrationBuilder.RenameColumn(
                name: "BranchId",
                table: "OrderFulfillmentLegs",
                newName: "BranchID");

            migrationBuilder.RenameColumn(
                name: "LegId",
                table: "OrderFulfillmentLegs",
                newName: "LegID");

            migrationBuilder.RenameIndex(
                name: "IX_OrderFulfillmentLegs_OrderId",
                table: "OrderFulfillmentLegs",
                newName: "IX_OrderFulfillmentLegs_OrderID");

            migrationBuilder.RenameIndex(
                name: "IX_OrderFulfillmentLegs_BranchId",
                table: "OrderFulfillmentLegs",
                newName: "IX_OrderFulfillmentLegs_BranchID");

            migrationBuilder.RenameColumn(
                name: "RxNormCui",
                table: "Drugs",
                newName: "RxNormCUI");

            migrationBuilder.RenameColumn(
                name: "DrugBankId",
                table: "Drugs",
                newName: "DrugBankID");

            migrationBuilder.RenameColumn(
                name: "DrugId",
                table: "Drugs",
                newName: "DrugID");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Addresses",
                newName: "UserID");

            migrationBuilder.RenameColumn(
                name: "AddressId",
                table: "Addresses",
                newName: "AddressID");

            migrationBuilder.RenameIndex(
                name: "IX_Addresses_UserId",
                table: "Addresses",
                newName: "IX_Addresses_UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Addresses_AspNetUsers_UserID",
                table: "Addresses",
                column: "UserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderFulfillmentLegs_Orders_OrderID",
                table: "OrderFulfillmentLegs",
                column: "OrderID",
                principalTable: "Orders",
                principalColumn: "OrderID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderFulfillmentLegs_PharmacyBranches_BranchID",
                table: "OrderFulfillmentLegs",
                column: "BranchID",
                principalTable: "PharmacyBranches",
                principalColumn: "BranchID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Drugs_DrugID",
                table: "OrderItems",
                column: "DrugID",
                principalTable: "Drugs",
                principalColumn: "DrugID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Orders_OrderID",
                table: "OrderItems",
                column: "OrderID",
                principalTable: "Orders",
                principalColumn: "OrderID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_PharmacyBranches_BranchID",
                table: "OrderItems",
                column: "BranchID",
                principalTable: "PharmacyBranches",
                principalColumn: "BranchID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Addresses_DeliveryAddressID",
                table: "Orders",
                column: "DeliveryAddressID",
                principalTable: "Addresses",
                principalColumn: "AddressID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_AspNetUsers_PatientUserID",
                table: "Orders",
                column: "PatientUserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pharmacies_AspNetUsers_OwnerUserID",
                table: "Pharmacies",
                column: "OwnerUserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PharmacyBranches_Pharmacies_PharmacyID",
                table: "PharmacyBranches",
                column: "PharmacyID",
                principalTable: "Pharmacies",
                principalColumn: "PharmacyID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PharmacyInventories_Drugs_DrugID",
                table: "PharmacyInventories",
                column: "DrugID",
                principalTable: "Drugs",
                principalColumn: "DrugID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PharmacyInventories_PharmacyBranches_BranchID",
                table: "PharmacyInventories",
                column: "BranchID",
                principalTable: "PharmacyBranches",
                principalColumn: "BranchID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
