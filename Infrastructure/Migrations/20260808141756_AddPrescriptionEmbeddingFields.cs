using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPrescriptionEmbeddingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmbeddedAt",
                table: "PrescriptionReviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmbeddingFailureReason",
                table: "PrescriptionReviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmbeddingStatus",
                table: "PrescriptionReviews",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmbeddedAt",
                table: "PrescriptionReviews");

            migrationBuilder.DropColumn(
                name: "EmbeddingFailureReason",
                table: "PrescriptionReviews");

            migrationBuilder.DropColumn(
                name: "EmbeddingStatus",
                table: "PrescriptionReviews");
        }
    }
}
