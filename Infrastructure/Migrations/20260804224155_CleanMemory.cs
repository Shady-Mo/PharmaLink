using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CleanMemory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DrugSuppliers_Drugs_DrugId",
                table: "DrugSuppliers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DrugSuppliers",
                table: "DrugSuppliers");

            migrationBuilder.RenameTable(
                name: "DrugSuppliers",
                newName: "DrugSupplier");

            migrationBuilder.RenameIndex(
                name: "IX_DrugSuppliers_DrugId",
                table: "DrugSupplier",
                newName: "IX_DrugSupplier_DrugId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DrugSupplier",
                table: "DrugSupplier",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DrugSupplier_Drugs_DrugId",
                table: "DrugSupplier",
                column: "DrugId",
                principalTable: "Drugs",
                principalColumn: "DrugId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
