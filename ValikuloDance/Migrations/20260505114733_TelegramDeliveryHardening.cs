using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ValikuloDance.Migrations
{
    /// <inheritdoc />
    public partial class TelegramDeliveryHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TelegramChatId",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateTable(
                name: "TelegramMessageDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecipientChatId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RecipientLogValue = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MessageType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    TelegramMessageId = table.Column<int>(type: "integer", nullable: true),
                    ErrorCode = table.Column<int>(type: "integer", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LastAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramMessageDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelegramMessageDeliveries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramMessageDeliveries_RelatedEntityId",
                table: "TelegramMessageDeliveries",
                column: "RelatedEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramMessageDeliveries_Status_CreatedAt",
                table: "TelegramMessageDeliveries",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramMessageDeliveries_UserId_CreatedAt",
                table: "TelegramMessageDeliveries",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramMessageDeliveries");

            migrationBuilder.AlterColumn<string>(
                name: "TelegramChatId",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
