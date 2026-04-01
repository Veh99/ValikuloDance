using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ValikuloDance.Migrations
{
    /// <inheritdoc />
    public partial class FKTrainerId2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trainers_Users_Id",
                table: "Trainers");

            migrationBuilder.RenameColumn(
                name: "TrainerId",
                table: "Trainers",
                newName: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Trainers_UserId",
                table: "Trainers",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Trainers_Users_UserId",
                table: "Trainers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trainers_Users_UserId",
                table: "Trainers");

            migrationBuilder.DropIndex(
                name: "IX_Trainers_UserId",
                table: "Trainers");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Trainers",
                newName: "TrainerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trainers_Users_Id",
                table: "Trainers",
                column: "Id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
