using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicalInquiriesAndReviewTeam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ProcessingStatus",
                table: "PrescriptionReviews",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "PendingPharmacistReview",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "MatchStatus",
                table: "PrescriptionReviewMedicines",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "NotFound",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateTable(
                name: "MedicalInquiries",
                columns: table => new
                {
                    MedicalInquiryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AnsweredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnsweredAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalInquiries", x => x.MedicalInquiryId);
                    table.ForeignKey(
                        name: "FK_MedicalInquiries_AspNetUsers_AnsweredByUserId",
                        column: x => x.AnsweredByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicalInquiries_AspNetUsers_PatientUserId",
                        column: x => x.PatientUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalInquiries_AnsweredByUserId",
                table: "MedicalInquiries",
                column: "AnsweredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalInquiries_PatientUserId",
                table: "MedicalInquiries",
                column: "PatientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalInquiries_Status_CreatedAt",
                table: "MedicalInquiries",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicalInquiries");

            migrationBuilder.AlterColumn<string>(
                name: "ProcessingStatus",
                table: "PrescriptionReviews",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "PendingPharmacistReview");

            migrationBuilder.AlterColumn<string>(
                name: "MatchStatus",
                table: "PrescriptionReviewMedicines",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "NotFound");
        }
    }
}
