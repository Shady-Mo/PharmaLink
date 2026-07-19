using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyPharmacyAdminToOnePharmacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PharmacyAdminPharmacies_AspNetUsers_PharmacyAdminId",
                table: "PharmacyAdminPharmacies");

            migrationBuilder.DropForeignKey(
                name: "FK_PharmacyAdminPharmacies_Pharmacies_PharmacyId",
                table: "PharmacyAdminPharmacies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PharmacyAdminPharmacies",
                table: "PharmacyAdminPharmacies");

            migrationBuilder.DropIndex(
                name: "IX_PharmacyAdminPharmacies_OnlyOneActivePharmacyPerAdmin",
                table: "PharmacyAdminPharmacies");

            migrationBuilder.DropIndex(
                name: "IX_PharmacyAdminPharmacies_PharmacyId",
                table: "PharmacyAdminPharmacies");

            migrationBuilder.DropIndex(
                name: "IX_PharmacyAdminPharmacies_UniqueAssignment",
                table: "PharmacyAdminPharmacies");

            migrationBuilder.RenameTable(
                name: "PharmacyAdminPharmacies",
                newName: "PharmacyAdminPharmacy");

            migrationBuilder.AddColumn<Guid>(
                name: "PharmacyId",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "PharmacyAdminPharmacy",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PharmacyAdminPharmacy",
                table: "PharmacyAdminPharmacy",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyAdmins_PharmacyId",
                table: "AspNetUsers",
                column: "PharmacyId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Pharmacies_PharmacyId",
                table: "AspNetUsers",
                column: "PharmacyId",
                principalTable: "Pharmacies",
                principalColumn: "PharmacyId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Pharmacies_PharmacyId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_PharmacyAdmins_PharmacyId",
                table: "AspNetUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PharmacyAdminPharmacy",
                table: "PharmacyAdminPharmacy");

            migrationBuilder.DropColumn(
                name: "PharmacyId",
                table: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "PharmacyAdminPharmacy",
                newName: "PharmacyAdminPharmacies");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "PharmacyAdminPharmacies",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PharmacyAdminPharmacies",
                table: "PharmacyAdminPharmacies",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyAdminPharmacies_OnlyOneActivePharmacyPerAdmin",
                table: "PharmacyAdminPharmacies",
                column: "PharmacyAdminId",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyAdminPharmacies_PharmacyId",
                table: "PharmacyAdminPharmacies",
                column: "PharmacyId");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyAdminPharmacies_UniqueAssignment",
                table: "PharmacyAdminPharmacies",
                columns: new[] { "PharmacyAdminId", "PharmacyId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PharmacyAdminPharmacies_AspNetUsers_PharmacyAdminId",
                table: "PharmacyAdminPharmacies",
                column: "PharmacyAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PharmacyAdminPharmacies_Pharmacies_PharmacyId",
                table: "PharmacyAdminPharmacies",
                column: "PharmacyId",
                principalTable: "Pharmacies",
                principalColumn: "PharmacyId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
