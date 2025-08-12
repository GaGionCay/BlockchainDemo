using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blockchain_Testing.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCVEntity3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "CVs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionId",
                table: "CVs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "CVs");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "CVs");
        }
    }
}
