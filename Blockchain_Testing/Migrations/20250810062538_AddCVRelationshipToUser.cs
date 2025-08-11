using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blockchain_Testing.Migrations
{
    /// <inheritdoc />
    public partial class AddCVRelationshipToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "BlockchainHash",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "CVs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CVs_UserId1",
                table: "CVs",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_CVs_Users_UserId1",
                table: "CVs",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CVs_Users_UserId1",
                table: "CVs");

            migrationBuilder.DropIndex(
                name: "IX_CVs_UserId1",
                table: "CVs");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "CVs");

            migrationBuilder.AlterColumn<string>(
                name: "BlockchainHash",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
