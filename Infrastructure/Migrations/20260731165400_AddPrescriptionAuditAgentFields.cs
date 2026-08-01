using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPrescriptionAuditAgentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AISummary",
                table: "PrescriptionReviews",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractedText",
                table: "PrescriptionReviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ExtractionConfidence",
                table: "PrescriptionReviews",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingStatus",
                table: "PrescriptionReviews",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "PendingPharmacistReview");

            migrationBuilder.AddColumn<string>(
                name: "MatchReason",
                table: "PrescriptionReviewMedicines",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MatchScore",
                table: "PrescriptionReviewMedicines",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchStatus",
                table: "PrescriptionReviewMedicines",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "NotFound");

            migrationBuilder.AddColumn<Guid>(
                name: "MatchedDrugId",
                table: "PrescriptionReviewMedicines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PatientApprovedAt",
                table: "PrescriptionReviewMedicines",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresPatientApproval",
                table: "PrescriptionReviewMedicines",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SuggestedAlternativeDrugId",
                table: "PrescriptionReviewMedicines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionReviewMedicines_MatchedDrugId",
                table: "PrescriptionReviewMedicines",
                column: "MatchedDrugId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionReviewMedicines_SuggestedAlternativeDrugId",
                table: "PrescriptionReviewMedicines",
                column: "SuggestedAlternativeDrugId");

            migrationBuilder.AddForeignKey(
                name: "FK_PrescriptionReviewMedicines_Drugs_MatchedDrugId",
                table: "PrescriptionReviewMedicines",
                column: "MatchedDrugId",
                principalTable: "Drugs",
                principalColumn: "DrugId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PrescriptionReviewMedicines_Drugs_SuggestedAlternativeDrugId",
                table: "PrescriptionReviewMedicines",
                column: "SuggestedAlternativeDrugId",
                principalTable: "Drugs",
                principalColumn: "DrugId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrescriptionReviewMedicines_Drugs_MatchedDrugId",
                table: "PrescriptionReviewMedicines");

            migrationBuilder.DropForeignKey(
                name: "FK_PrescriptionReviewMedicines_Drugs_SuggestedAlternativeDrugId",
                table: "PrescriptionReviewMedicines");

            migrationBuilder.DropIndex(
                name: "IX_PrescriptionReviewMedicines_MatchedDrugId",
                table: "PrescriptionReviewMedicines");

            migrationBuilder.DropIndex(
                name: "IX_PrescriptionReviewMedicines_SuggestedAlternativeDrugId",
                table: "PrescriptionReviewMedicines");

            migrationBuilder.DropColumn(
                name: "AISummary",
                table: "PrescriptionReviews");

            migrationBuilder.DropColumn(
                name: "ExtractedText",
                table: "PrescriptionReviews");

            migrationBuilder.DropColumn(
                name: "ExtractionConfidence",
                table: "PrescriptionReviews");

            migrationBuilder.DropColumn(
                name: "ProcessingStatus",
                table: "PrescriptionReviews");

            migrationBuilder.DropColumn(
                name: "MatchReason",
                table: "PrescriptionReviewMedicines");

            migrationBuilder.DropColumn(
                name: "MatchScore",
                table: "PrescriptionReviewMedicines");

            migrationBuilder.DropColumn(
                name: "MatchStatus",
                table: "PrescriptionReviewMedicines");

            migrationBuilder.DropColumn(
                name: "MatchedDrugId",
                table: "PrescriptionReviewMedicines");

            migrationBuilder.DropColumn(
                name: "PatientApprovedAt",
                table: "PrescriptionReviewMedicines");

            migrationBuilder.DropColumn(
                name: "RequiresPatientApproval",
                table: "PrescriptionReviewMedicines");

            migrationBuilder.DropColumn(
                name: "SuggestedAlternativeDrugId",
                table: "PrescriptionReviewMedicines");

        }
    }
}
