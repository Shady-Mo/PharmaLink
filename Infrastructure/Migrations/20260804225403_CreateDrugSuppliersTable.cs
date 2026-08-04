using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateDrugSuppliersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                 name: "DrugSuppliers",
                 columns: table => new
                 {
                     Id = table.Column<int>(type: "int", nullable: false)
                         .Annotation("SqlServer:Identity", "1, 1"),
                     DrugId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                     SupplierId = table.Column<int>(type: "int", nullable: false),
                     NameAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                     NameEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                     Discount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                     CommercialPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                     Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                     Quantity = table.Column<int>(type: "int", nullable: false)
                 },
                 constraints: table =>
                 {
                     table.PrimaryKey("PK_DrugSuppliers", x => x.Id);
                     table.ForeignKey(
                         name: "FK_DrugSuppliers_Drugs_DrugId",
                         column: x => x.DrugId,
                         principalTable: "Drugs",
                         principalColumn: "DrugId",
                         onDelete: ReferentialAction.Cascade);
                 });
            migrationBuilder.CreateIndex(
                name: "IX_DrugSuppliers_DrugId",
                table: "DrugSuppliers",
                column: "DrugId");
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
