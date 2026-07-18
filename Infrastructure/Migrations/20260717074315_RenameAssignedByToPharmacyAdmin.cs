using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameAssignedByToPharmacyAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PharmacistAssignments_AspNetUsers_AssignedByAdminId",
                table: "PharmacistAssignments");

            migrationBuilder.DropTable(
                name: "PharmacyAdminPharmacy");

            migrationBuilder.RenameColumn(
                name: "AssignedByAdminId",
                table: "PharmacistAssignments",
                newName: "AssignedByPharmacyAdminId");

            migrationBuilder.RenameIndex(
                name: "IX_PharmacistAssignments_AssignedByAdminId",
                table: "PharmacistAssignments",
                newName: "IX_PharmacistAssignments_AssignedByPharmacyAdminId");

            migrationBuilder.AlterColumn<bool>(
                name: "IsSuperAdmin",
                table: "AspNetUsers",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true,
                oldDefaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_PharmacistAssignments_AspNetUsers_AssignedByPharmacyAdminId",
                table: "PharmacistAssignments",
                column: "AssignedByPharmacyAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PharmacistAssignments_AspNetUsers_AssignedByPharmacyAdminId",
                table: "PharmacistAssignments");

            migrationBuilder.RenameColumn(
                name: "AssignedByPharmacyAdminId",
                table: "PharmacistAssignments",
                newName: "AssignedByAdminId");

            migrationBuilder.RenameIndex(
                name: "IX_PharmacistAssignments_AssignedByPharmacyAdminId",
                table: "PharmacistAssignments",
                newName: "IX_PharmacistAssignments_AssignedByAdminId");

            migrationBuilder.AlterColumn<bool>(
                name: "IsSuperAdmin",
                table: "AspNetUsers",
                type: "bit",
                nullable: true,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "PharmacyAdminPharmacy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PharmacyAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PharmacyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PharmacyAdminPharmacy", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_PharmacistAssignments_AspNetUsers_AssignedByAdminId",
                table: "PharmacistAssignments",
                column: "AssignedByAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
