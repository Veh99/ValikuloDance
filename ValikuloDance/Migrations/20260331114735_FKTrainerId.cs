using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ValikuloDance.Migrations
{
    /// <inheritdoc />
    public partial class FKTrainerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trainers_Users_TrainerId",
                table: "Trainers");

            migrationBuilder.DropIndex(
                name: "IX_Trainers_TrainerId",
                table: "Trainers");

            migrationBuilder.AddForeignKey(
                name: "FK_Trainers_Users_Id",
                table: "Trainers",
                column: "Id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trainers_Users_Id",
                table: "Trainers");

            migrationBuilder.CreateIndex(
                name: "IX_Trainers_TrainerId",
                table: "Trainers",
                column: "TrainerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trainers_Users_TrainerId",
                table: "Trainers",
                column: "TrainerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
