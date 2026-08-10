using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchToPharmacistAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "PharmacistAssignments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_PharmacistAssignments_BranchId",
                table: "PharmacistAssignments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacistAssignments_BranchId_Active",
                table: "PharmacistAssignments",
                columns: new[] { "BranchId", "IsActive" },
                filter: "[IsActive] = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_PharmacistAssignments_PharmacyBranches_BranchId",
                table: "PharmacistAssignments",
                column: "BranchId",
                principalTable: "PharmacyBranches",
                principalColumn: "BranchId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PharmacistAssignments_PharmacyBranches_BranchId",
                table: "PharmacistAssignments");

            migrationBuilder.DropIndex(
                name: "IX_PharmacistAssignments_BranchId",
                table: "PharmacistAssignments");

            migrationBuilder.DropIndex(
                name: "IX_PharmacistAssignments_BranchId_Active",
                table: "PharmacistAssignments");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "PharmacistAssignments");
        }
    }
}
