using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RestrictPharmacyAdminToAtMostOnePharmacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PharmacyAdminPharmacies_OnlyOneActivePharmacyPerAdmin",
                table: "PharmacyAdminPharmacies",
                column: "PharmacyAdminId",
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PharmacyAdminPharmacies_OnlyOneActivePharmacyPerAdmin",
                table: "PharmacyAdminPharmacies");
        }
    }
}
