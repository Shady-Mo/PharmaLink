using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingStockReportsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PharmacyMissingStockLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PharmacyId = table.Column<int>(type: "int", nullable: false),
                    DrugId = table.Column<int>(type: "int", nullable: false),
                    QuantityRequested = table.Column<int>(type: "int", nullable: false),
                    LoggedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false),
                    PharmacyId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DrugId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PharmacyMissingStockLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PharmacyMissingStockLog_Drugs_DrugId1",
                        column: x => x.DrugId1,
                        principalTable: "Drugs",
                        principalColumn: "DrugId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PharmacyMissingStockLog_Pharmacies_PharmacyId1",
                        column: x => x.PharmacyId1,
                        principalTable: "Pharmacies",
                        principalColumn: "PharmacyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PharmacyReport",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PharmacyId = table.Column<int>(type: "int", nullable: false),
                    ReportTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDownloaded = table.Column<bool>(type: "bit", nullable: false),
                    PharmacyId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PharmacyReport", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PharmacyReport_Pharmacies_PharmacyId1",
                        column: x => x.PharmacyId1,
                        principalTable: "Pharmacies",
                        principalColumn: "PharmacyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyMissingStockLog_DrugId1",
                table: "PharmacyMissingStockLog",
                column: "DrugId1");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyMissingStockLog_PharmacyId1",
                table: "PharmacyMissingStockLog",
                column: "PharmacyId1");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyReport_PharmacyId1",
                table: "PharmacyReport",
                column: "PharmacyId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PharmacyMissingStockLog");

            migrationBuilder.DropTable(
                name: "PharmacyReport");
        }
    }
}
