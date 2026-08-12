using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedDrugFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DrugBankId",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "GameballPoints",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "NdcCode",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "RawJson",
                table: "Drugs");

            migrationBuilder.DropColumn(
                name: "RxNormCui",
                table: "Drugs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DrugBankId",
                table: "Drugs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "GameballPoints",
                table: "Drugs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NdcCode",
                table: "Drugs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RawJson",
                table: "Drugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RxNormCui",
                table: "Drugs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }
    }
}
