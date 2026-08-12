using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAdditionalUnusedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BundleTagAr",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "BundleTagEn",
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
                name: "DrugClass",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "GenericName",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "SortingKeywordAr",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "SortingKeywordEn",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "Strength",
                table: "Drugs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<string>(
                name: "DrugClass",
                table: "Drugs",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GenericName",
                table: "Drugs",
                type: "nvarchar(2000)",
                maxLength: 2000,
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
                name: "Strength",
                table: "Drugs",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");
        }
    }
}
