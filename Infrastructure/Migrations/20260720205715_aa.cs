using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class aa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PharmacyId1",
                table: "PharmacistAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PharmacistAssignments_PharmacyId1",
                table: "PharmacistAssignments",
                column: "PharmacyId1");

            migrationBuilder.AddForeignKey(
                name: "FK_PharmacistAssignments_Pharmacies_PharmacyId1",
                table: "PharmacistAssignments",
                column: "PharmacyId1",
                principalTable: "Pharmacies",
                principalColumn: "PharmacyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PharmacistAssignments_Pharmacies_PharmacyId1",
                table: "PharmacistAssignments");

            migrationBuilder.DropIndex(
                name: "IX_PharmacistAssignments_PharmacyId1",
                table: "PharmacistAssignments");

            migrationBuilder.DropColumn(
                name: "PharmacyId1",
                table: "PharmacistAssignments");
        }
    }
}
