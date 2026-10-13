using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HighConvertAI.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddingTheHasingHtmlColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HtmlHash",
                table: "PageScans",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HtmlHash",
                table: "PageScans");
        }
    }
}
