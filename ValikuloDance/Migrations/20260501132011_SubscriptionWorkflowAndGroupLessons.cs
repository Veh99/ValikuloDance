using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ValikuloDance.Migrations
{
    /// <inheritdoc />
    public partial class SubscriptionWorkflowAndGroupLessons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Services_ServiceId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_UserId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_UserId",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "ServiceId",
                table: "Subscriptions",
                newName: "SubscriptionPlanId");

            migrationBuilder.RenameIndex(
                name: "IX_Subscriptions_ServiceId",
                table: "Subscriptions",
                newName: "IX_Subscriptions_SubscriptionPlanId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiresAt",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDeadlineAt",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Subscriptions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedAt",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "StartsAt",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Subscriptions",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Format",
                table: "Services",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Individual");

            migrationBuilder.AddColumn<Guid>(
                name: "GroupLessonSlotId",
                table: "Bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSubscriptionSessionConsumed",
                table: "Bookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMode",
                table: "Bookings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Single");

            migrationBuilder.AddColumn<Guid>(
                name: "SubscriptionId",
                table: "Bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GroupLessonSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupLessonSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupLessonSlots_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupLessonSlots_Trainers_TrainerId",
                        column: x => x.TrainerId,
                        principalTable: "Trainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Format = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SessionsCount = table.Column<int>(type: "integer", nullable: false),
                    ValidityMonths = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    SourceServiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionPlans_Services_SourceServiceId",
                        column: x => x.SourceServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PaymentDeadlineAt",
                table: "Subscriptions",
                column: "PaymentDeadlineAt");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserId_Status",
                table: "Subscriptions",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_GroupLessonSlotId",
                table: "Bookings",
                column: "GroupLessonSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_SubscriptionId",
                table: "Bookings",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_UserId_SubscriptionId",
                table: "Bookings",
                columns: new[] { "UserId", "SubscriptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupLessonSlots_ServiceId_TrainerId_StartTime",
                table: "GroupLessonSlots",
                columns: new[] { "ServiceId", "TrainerId", "StartTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupLessonSlots_TrainerId",
                table: "GroupLessonSlots",
                column: "TrainerId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_SourceServiceId",
                table: "SubscriptionPlans",
                column: "SourceServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_GroupLessonSlots_GroupLessonSlotId",
                table: "Bookings",
                column: "GroupLessonSlotId",
                principalTable: "GroupLessonSlots",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Subscriptions_SubscriptionId",
                table: "Bookings",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_SubscriptionPlans_SubscriptionPlanId",
                table: "Subscriptions",
                column: "SubscriptionPlanId",
                principalTable: "SubscriptionPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_GroupLessonSlots_GroupLessonSlotId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Subscriptions_SubscriptionId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_SubscriptionPlans_SubscriptionPlanId",
                table: "Subscriptions");

            migrationBuilder.DropTable(
                name: "GroupLessonSlots");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_PaymentDeadlineAt",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_UserId_Status",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_GroupLessonSlotId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_SubscriptionId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_UserId_SubscriptionId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "PaymentDeadlineAt",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "RequestedAt",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "StartsAt",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "Format",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "GroupLessonSlotId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IsSubscriptionSessionConsumed",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PaymentMode",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "SubscriptionPlanId",
                table: "Subscriptions",
                newName: "ServiceId");

            migrationBuilder.RenameIndex(
                name: "IX_Subscriptions_SubscriptionPlanId",
                table: "Subscriptions",
                newName: "IX_Subscriptions_ServiceId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiresAt",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserId",
                table: "Subscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_UserId",
                table: "Bookings",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Services_ServiceId",
                table: "Subscriptions",
                column: "ServiceId",
                principalTable: "Services",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
