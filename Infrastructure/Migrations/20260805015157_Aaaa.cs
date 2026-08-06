using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Aaaa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierDrug_AspNetUsers_SupplierId",
                table: "SupplierDrug");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierDrug_Drugs_DrugId",
                table: "SupplierDrug");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SupplierDrug",
                table: "SupplierDrug");

            migrationBuilder.RenameTable(
                name: "SupplierDrug",
                newName: "SupplierDrugs");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierDrug_SupplierId_DrugId",
                table: "SupplierDrugs",
                newName: "IX_SupplierDrugs_SupplierId_DrugId");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierDrug_DrugId",
                table: "SupplierDrugs",
                newName: "IX_SupplierDrugs_DrugId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SupplierDrugs",
                table: "SupplierDrugs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierDrugs_AspNetUsers_SupplierId",
                table: "SupplierDrugs",
                column: "SupplierId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierDrugs_Drugs_DrugId",
                table: "SupplierDrugs",
                column: "DrugId",
                principalTable: "Drugs",
                principalColumn: "DrugId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierDrugs_AspNetUsers_SupplierId",
                table: "SupplierDrugs");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierDrugs_Drugs_DrugId",
                table: "SupplierDrugs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SupplierDrugs",
                table: "SupplierDrugs");

            migrationBuilder.RenameTable(
                name: "SupplierDrugs",
                newName: "SupplierDrug");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierDrugs_SupplierId_DrugId",
                table: "SupplierDrug",
                newName: "IX_SupplierDrug_SupplierId_DrugId");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierDrugs_DrugId",
                table: "SupplierDrug",
                newName: "IX_SupplierDrug_DrugId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SupplierDrug",
                table: "SupplierDrug",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierDrug_AspNetUsers_SupplierId",
                table: "SupplierDrug",
                column: "SupplierId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierDrug_Drugs_DrugId",
                table: "SupplierDrug",
                column: "DrugId",
                principalTable: "Drugs",
                principalColumn: "DrugId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
