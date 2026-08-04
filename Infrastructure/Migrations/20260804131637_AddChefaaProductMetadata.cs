using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChefaaProductMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.DropForeignKey(
            //     name: "FK_PurchaseOrder_Drugs_DrugId",
            //     table: "PurchaseOrder");

            // migrationBuilder.DropForeignKey(
            //     name: "FK_PurchaseOrder_PharmacyBranches_BranchId",
            //     table: "PurchaseOrder");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Drugs");

            // migrationBuilder.RenameTable(
            //     name: "PurchaseOrder",
            //     newName: "PurchaseOrders");

            migrationBuilder.AddColumn<string>(
                name: "BrandAr",
                table: "Drugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BrandEn",
                table: "Drugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BrandImageUrl",
                table: "Drugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BrandSlug",
                table: "Drugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BundleTagAr",
                table: "Drugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BundleTagEn",
                table: "Drugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Drugs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ChefaaId",
                table: "Drugs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostPrice",
                table: "Drugs",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CouponDescriptionAr",
                table: "Drugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CouponDescriptionEn",
                table: "Drugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionAr",
                table: "Drugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "Drugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                table: "Drugs",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalPrice",
                table: "Drugs",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "FlowType",
                table: "Drugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FullUrl",
                table: "Drugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "GameballPoints",
                table: "Drugs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InStock",
                table: "Drugs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LowStock",
                table: "Drugs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxQuantity",
                table: "Drugs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MetaDescriptionAr",
                table: "Drugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MetaDescriptionEn",
                table: "Drugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MetaKeywordsAr",
                table: "Drugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MetaKeywordsEn",
                table: "Drugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "OutOfStock",
                table: "Drugs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PurchaseCount",
                table: "Drugs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Drugs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RawJson",
                table: "Drugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Drugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SortingKeywordAr",
                table: "Drugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SortingKeywordEn",
                table: "Drugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Drugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Drugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            // PurchaseOrders column modifications skipped

            migrationBuilder.CreateTable(
                name: "DrugCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrugCategories_DrugCategories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "DrugCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DrugLandingPages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DrugId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugLandingPages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrugLandingPages_Drugs_DrugId",
                        column: x => x.DrugId,
                        principalTable: "Drugs",
                        principalColumn: "DrugId",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "IX_Drugs_CategoryId",
                table: "Drugs",
                column: "CategoryId");

            // PurchaseOrders indexes skipped

            migrationBuilder.CreateIndex(
                name: "IX_DrugCategories_ParentId",
                table: "DrugCategories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugLandingPages_DrugId",
                table: "DrugLandingPages",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugSuppliers_DrugId",
                table: "DrugSuppliers",
                column: "DrugId");

            migrationBuilder.AddForeignKey(
                name: "FK_Drugs_DrugCategories_CategoryId",
                table: "Drugs",
                column: "CategoryId",
                principalTable: "DrugCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // PurchaseOrders FKs skipped
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drugs_DrugCategories_CategoryId",
                table: "Drugs");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Drugs_DrugId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_PharmacyBranches_BranchId",
                table: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "DrugCategories");

            migrationBuilder.DropTable(
                name: "DrugLandingPages");

            migrationBuilder.DropTable(
                name: "DrugSuppliers");

            migrationBuilder.DropIndex(
                name: "IX_Drugs_CategoryId",
                table: "Drugs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PurchaseOrders",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_BranchId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_DrugId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "BrandAr",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "BrandEn",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "BrandImageUrl",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "BrandSlug",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "BundleTagAr",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "BundleTagEn",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "ChefaaId",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "CostPrice",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "CouponDescriptionAr",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "CouponDescriptionEn",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "DescriptionAr",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "Discount",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "FinalPrice",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "FlowType",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "FullUrl",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "GameballPoints",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "InStock",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "LowStock",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "MaxQuantity",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "MetaDescriptionAr",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "MetaDescriptionEn",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "MetaKeywordsAr",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "MetaKeywordsEn",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "OutOfStock",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "PurchaseCount",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "RawJson",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "SortingKeywordAr",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "SortingKeywordEn",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "AiRationale",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "OrderedQuantity",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PurchaseOrders");

            migrationBuilder.RenameTable(
                name: "PurchaseOrders",
                newName: "PurchaseOrder");

            migrationBuilder.AddColumn<byte>(
                name: "Category",
                table: "Drugs",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AlterColumn<Guid>(
                name: "DrugId",
                table: "PurchaseOrder",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "BranchId",
                table: "PurchaseOrder",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrder_Drugs_DrugId",
                table: "PurchaseOrder",
                column: "DrugId",
                principalTable: "Drugs",
                principalColumn: "DrugId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrder_PharmacyBranches_BranchId",
                table: "PurchaseOrder",
                column: "BranchId",
                principalTable: "PharmacyBranches",
                principalColumn: "BranchId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
