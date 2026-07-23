using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArabic_CI_AICollationForBetterMatchingWithArabicNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Governorate",
                table: "PharmacyBranches",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                collation: "Arabic_CI_AI",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "PharmacyBranches",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                collation: "Arabic_CI_AI",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "BranchName",
                table: "PharmacyBranches",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                collation: "Arabic_CI_AI",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "AddressLine",
                table: "PharmacyBranches",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                collation: "Arabic_CI_AI",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Governorate",
                table: "PharmacyBranches",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldCollation: "Arabic_CI_AI");

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "PharmacyBranches",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldCollation: "Arabic_CI_AI");

            migrationBuilder.AlterColumn<string>(
                name: "BranchName",
                table: "PharmacyBranches",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldCollation: "Arabic_CI_AI");

            migrationBuilder.AlterColumn<string>(
                name: "AddressLine",
                table: "PharmacyBranches",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250,
                oldCollation: "Arabic_CI_AI");
        }
    }
}
