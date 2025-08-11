using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blockchain_Testing.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCVEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "CVs",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "CVs");
        }
    }
}
