using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BreakingScoreBoard.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBirthYearFieldsToCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxBirthYear",
                table: "AgeCategories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinBirthYear",
                table: "AgeCategories",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxBirthYear",
                table: "AgeCategories");

            migrationBuilder.DropColumn(
                name: "MinBirthYear",
                table: "AgeCategories");
        }
    }
}
