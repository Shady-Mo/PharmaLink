using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToPharmacyInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PharmacyInventories_BranchId",
                table: "PharmacyInventories");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyInventories_BranchId_DrugId",
                table: "PharmacyInventories",
                columns: new[] { "BranchId", "DrugId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PharmacyInventories_BranchId_DrugId",
                table: "PharmacyInventories");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyInventories_BranchId",
                table: "PharmacyInventories",
                column: "BranchId");
        }
    }
}
