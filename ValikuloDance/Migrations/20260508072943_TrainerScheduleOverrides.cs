using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ValikuloDance.Migrations
{
    /// <inheritdoc />
    public partial class TrainerScheduleOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrainerScheduleOverrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    StartTimeLocal = table.Column<TimeSpan>(type: "interval", nullable: true),
                    EndTimeLocal = table.Column<TimeSpan>(type: "interval", nullable: true),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SlotDurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    Reason = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainerScheduleOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainerScheduleOverrides_Trainers_TrainerId",
                        column: x => x.TrainerId,
                        principalTable: "Trainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainerScheduleOverrides_TrainerId_Date_IsActive",
                table: "TrainerScheduleOverrides",
                columns: new[] { "TrainerId", "Date", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainerScheduleOverrides");
        }
    }
}
