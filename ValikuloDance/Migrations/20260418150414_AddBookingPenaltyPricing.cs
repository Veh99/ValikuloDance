using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ValikuloDance.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingPenaltyPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasLateCancellationPenalty",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceAtBooking",
                table: "Bookings",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasLateCancellationPenalty",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PriceAtBooking",
                table: "Bookings");
        }
    }
}
