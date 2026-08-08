using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class prescriptionRAG : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrescriptionVectorIndices",
                columns: table => new
                {
                    PrescriptionVectorIndexId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrescriptionReviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Governorate = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IndexedText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmbeddingJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MedicinesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPediatric = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionVectorIndices", x => x.PrescriptionVectorIndexId);
                    table.ForeignKey(
                        name: "FK_PrescriptionVectorIndices_PharmacyBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "PharmacyBranches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PrescriptionVectorIndices_PrescriptionReviews_PrescriptionReviewId",
                        column: x => x.PrescriptionReviewId,
                        principalTable: "PrescriptionReviews",
                        principalColumn: "PrescriptionReviewId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionVectorIndices_BranchId_CreatedAt",
                table: "PrescriptionVectorIndices",
                columns: new[] { "BranchId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionVectorIndices_City_CreatedAt",
                table: "PrescriptionVectorIndices",
                columns: new[] { "City", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionVectorIndices_PrescriptionReviewId",
                table: "PrescriptionVectorIndices",
                column: "PrescriptionReviewId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrescriptionVectorIndices");
        }
    }
}
