using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HighConvertAI.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddingScroreProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Score",
                table: "PageScans",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Score",
                table: "PageScans");
        }
    }
}
