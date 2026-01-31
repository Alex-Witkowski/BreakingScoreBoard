using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BreakingScoreBoard.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDisplayNameToBreaker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "Breakers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "Breakers");
        }
    }
}
