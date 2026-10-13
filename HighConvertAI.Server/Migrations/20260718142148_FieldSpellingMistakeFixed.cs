using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HighConvertAI.Server.Migrations
{
    /// <inheritdoc />
    public partial class FieldSpellingMistakeFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AffectedRevinueOpportunity",
                table: "IssueSeveritys",
                newName: "AffectedRevenueOpportunity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AffectedRevenueOpportunity",
                table: "IssueSeveritys",
                newName: "AffectedRevinueOpportunity");
        }
    }
}
