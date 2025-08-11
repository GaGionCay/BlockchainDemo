using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blockchain_Testing.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCVEntity2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicKey",
                table: "CVs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Signature",
                table: "CVs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublicKey",
                table: "CVs");

            migrationBuilder.DropColumn(
                name: "Signature",
                table: "CVs");
        }
    }
}
