using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CleanMemory2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        { }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DrugSupplier_Drugs_DrugId",
                table: "DrugSupplier");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DrugSupplier",
                table: "DrugSupplier");

            migrationBuilder.RenameTable(
                name: "DrugSupplier",
                newName: "DrugSuppliers");

            migrationBuilder.RenameIndex(
                name: "IX_DrugSupplier_DrugId",
                table: "DrugSuppliers",
                newName: "IX_DrugSuppliers_DrugId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DrugSuppliers",
                table: "DrugSuppliers",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DrugSuppliers_Drugs_DrugId",
                table: "DrugSuppliers",
                column: "DrugId",
                principalTable: "Drugs",
                principalColumn: "DrugId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
