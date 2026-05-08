using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ValikuloDance.Migrations
{
    /// <inheritdoc />
    public partial class GroupLessonSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GroupLessonScheduleId",
                table: "GroupLessonSlots",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GroupLessonSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartTimeLocal = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupLessonSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupLessonSchedules_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupLessonSchedules_Trainers_TrainerId",
                        column: x => x.TrainerId,
                        principalTable: "Trainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupLessonSlots_GroupLessonScheduleId",
                table: "GroupLessonSlots",
                column: "GroupLessonScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupLessonSchedules_ServiceId",
                table: "GroupLessonSchedules",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupLessonSchedules_TrainerId_ServiceId_DayOfWeek_StartTim~",
                table: "GroupLessonSchedules",
                columns: new[] { "TrainerId", "ServiceId", "DayOfWeek", "StartTimeLocal" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupLessonSlots_GroupLessonSchedules_GroupLessonScheduleId",
                table: "GroupLessonSlots",
                column: "GroupLessonScheduleId",
                principalTable: "GroupLessonSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupLessonSlots_GroupLessonSchedules_GroupLessonScheduleId",
                table: "GroupLessonSlots");

            migrationBuilder.DropTable(
                name: "GroupLessonSchedules");

            migrationBuilder.DropIndex(
                name: "IX_GroupLessonSlots_GroupLessonScheduleId",
                table: "GroupLessonSlots");

            migrationBuilder.DropColumn(
                name: "GroupLessonScheduleId",
                table: "GroupLessonSlots");
        }
    }
}
