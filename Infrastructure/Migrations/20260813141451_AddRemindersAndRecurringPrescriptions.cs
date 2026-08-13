using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRemindersAndRecurringPrescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MedicineReminders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicineName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Dosage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReminderTimesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PrescriptionReviewMedicineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NotifyByEmail = table.Column<bool>(type: "bit", nullable: false),
                    NotifyByWhatsApp = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicineReminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicineReminders_AspNetUsers_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecurringPrescriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrescriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IntervalDays = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    NextRunDate = table.Column<DateOnly>(type: "date", nullable: false),
                    FulfillmentMode = table.Column<byte>(type: "tinyint", nullable: false),
                    PreferredBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeliveryAddressId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequireConfirmation = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringPrescriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringPrescriptions_AspNetUsers_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecurringPrescriptions_PharmacyBranches_PreferredBranchId",
                        column: x => x.PreferredBranchId,
                        principalTable: "PharmacyBranches",
                        principalColumn: "BranchId");
                    table.ForeignKey(
                        name: "FK_RecurringPrescriptions_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalTable: "Prescriptions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MedicineReminderLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReminderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicineReminderLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicineReminderLogs_MedicineReminders_ReminderId",
                        column: x => x.ReminderId,
                        principalTable: "MedicineReminders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecurringPrescriptionRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecurringPrescriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConfirmationToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConfirmationDeadline = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringPrescriptionRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringPrescriptionRuns_RecurringPrescriptions_RecurringPrescriptionId",
                        column: x => x.RecurringPrescriptionId,
                        principalTable: "RecurringPrescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicineReminderLogs_ReminderId",
                table: "MedicineReminderLogs",
                column: "ReminderId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineReminders_PatientId",
                table: "MedicineReminders",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringPrescriptionRuns_RecurringPrescriptionId",
                table: "RecurringPrescriptionRuns",
                column: "RecurringPrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringPrescriptions_PatientId",
                table: "RecurringPrescriptions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringPrescriptions_PreferredBranchId",
                table: "RecurringPrescriptions",
                column: "PreferredBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringPrescriptions_PrescriptionId",
                table: "RecurringPrescriptions",
                column: "PrescriptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicineReminderLogs");

            migrationBuilder.DropTable(
                name: "RecurringPrescriptionRuns");

            migrationBuilder.DropTable(
                name: "MedicineReminders");

            migrationBuilder.DropTable(
                name: "RecurringPrescriptions");
        }
    }
}
