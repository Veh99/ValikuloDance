using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ValikuloDance.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainerWorkingHoursAndMaterializedScheduleSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleSlots_Bookings_Id",
                table: "ScheduleSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_Trainers_Users_UserId",
                table: "Trainers");

            migrationBuilder.AddColumn<Guid>(
                name: "BookingId",
                table: "ScheduleSlots",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "ScheduleSlots",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "TrainerWorkingHours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartTimeLocal = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTimeLocal = table.Column<TimeSpan>(type: "interval", nullable: false),
                    SlotDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainerWorkingHours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainerWorkingHours_Trainers_TrainerId",
                        column: x => x.TrainerId,
                        principalTable: "Trainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSlots_BookingId",
                table: "ScheduleSlots",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSlots_TrainerId_StartTime_IsBooked",
                table: "ScheduleSlots",
                columns: new[] { "TrainerId", "StartTime", "IsBooked" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainerWorkingHours_TrainerId_DayOfWeek_StartTimeLocal_EndT~",
                table: "TrainerWorkingHours",
                columns: new[] { "TrainerId", "DayOfWeek", "StartTimeLocal", "EndTimeLocal" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleSlots_Bookings_BookingId",
                table: "ScheduleSlots",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Trainers_Users_UserId",
                table: "Trainers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleSlots_Bookings_BookingId",
                table: "ScheduleSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_Trainers_Users_UserId",
                table: "Trainers");

            migrationBuilder.DropTable(
                name: "TrainerWorkingHours");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleSlots_BookingId",
                table: "ScheduleSlots");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleSlots_TrainerId_StartTime_IsBooked",
                table: "ScheduleSlots");

            migrationBuilder.DropColumn(
                name: "BookingId",
                table: "ScheduleSlots");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "ScheduleSlots");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleSlots_Bookings_Id",
                table: "ScheduleSlots",
                column: "Id",
                principalTable: "Bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Trainers_Users_UserId",
                table: "Trainers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
