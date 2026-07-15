using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPrescriptionReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrescriptionReviews",
                columns: table => new
                {
                    PrescriptionReviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrescriptionImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AIModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReviewStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    PharmacistUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionReviews", x => x.PrescriptionReviewId);
                    table.ForeignKey(
                        name: "FK_PrescriptionReviews_AspNetUsers_PatientUserId",
                        column: x => x.PatientUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrescriptionReviews_AspNetUsers_PharmacistUserId",
                        column: x => x.PharmacistUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PrescriptionReviews_Orders_CreatedOrderId",
                        column: x => x.CreatedOrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionReviewMedicines",
                columns: table => new
                {
                    PrescriptionReviewMedicineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrescriptionReviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicineName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OriginalMedicineName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GenericName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Strength = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DosageForm = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Dose = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Frequency = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Duration = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Route = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Confidence = table.Column<double>(type: "float", nullable: true),
                    IsEdited = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionReviewMedicines", x => x.PrescriptionReviewMedicineId);
                    table.ForeignKey(
                        name: "FK_PrescriptionReviewMedicines_PrescriptionReviews_PrescriptionReviewId",
                        column: x => x.PrescriptionReviewId,
                        principalTable: "PrescriptionReviews",
                        principalColumn: "PrescriptionReviewId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionReviewMedicines_PrescriptionReviewId",
                table: "PrescriptionReviewMedicines",
                column: "PrescriptionReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionReviews_CreatedOrderId",
                table: "PrescriptionReviews",
                column: "CreatedOrderId",
                unique: true,
                filter: "[CreatedOrderId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionReviews_PatientUserId",
                table: "PrescriptionReviews",
                column: "PatientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionReviews_PharmacistUserId",
                table: "PrescriptionReviews",
                column: "PharmacistUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrescriptionReviewMedicines");

            migrationBuilder.DropTable(
                name: "PrescriptionReviews");
        }
    }
}
