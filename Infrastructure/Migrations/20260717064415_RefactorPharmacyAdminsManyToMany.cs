using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorPharmacyAdminsManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Pharmacies_PharmacyId",
                table: "AspNetUsers");

            //migrationBuilder.DropForeignKey(
            //    name: "FK_Pharmacies_AspNetUsers_OwnerUserId",
            //    table: "Pharmacies");

            //migrationBuilder.DropIndex(
            //    name: "IX_Pharmacies_OwnerUserId",
            //    table: "Pharmacies");

            migrationBuilder.DropIndex(
                name: "IX_PharmacyAdmins_PharmacyId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PharmacyId",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<bool>(
                name: "IsSuperAdmin",
                table: "AspNetUsers",
                type: "bit",
                nullable: true,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PharmacyAdminPharmacies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PharmacyAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PharmacyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PharmacyAdminPharmacies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PharmacyAdminPharmacies_AspNetUsers_PharmacyAdminId",
                        column: x => x.PharmacyAdminId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PharmacyAdminPharmacies_Pharmacies_PharmacyId",
                        column: x => x.PharmacyId,
                        principalTable: "Pharmacies",
                        principalColumn: "PharmacyId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyAdminPharmacies_PharmacyId",
                table: "PharmacyAdminPharmacies",
                column: "PharmacyId");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyAdminPharmacies_UniqueAssignment",
                table: "PharmacyAdminPharmacies",
                columns: new[] { "PharmacyAdminId", "PharmacyId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PharmacyAdminPharmacies");

            migrationBuilder.DropColumn(
                name: "IsSuperAdmin",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<Guid>(
                name: "PharmacyId",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pharmacies_OwnerUserId",
                table: "Pharmacies",
                column: "OwnerUserId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Pharmacies_AspNetUsers_OwnerUserId",
                table: "Pharmacies",
                column: "OwnerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
