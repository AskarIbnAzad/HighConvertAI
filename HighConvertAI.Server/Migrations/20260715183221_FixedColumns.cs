using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HighConvertAI.Server.Migrations
{
    /// <inheritdoc />
    public partial class FixedColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Browser",
                table: "PageScans");

            migrationBuilder.DropColumn(
                name: "City",
                table: "PageScans");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "PageScans");

            migrationBuilder.DropColumn(
                name: "DeviceType",
                table: "PageScans");

            migrationBuilder.DropColumn(
                name: "OperatingSystem",
                table: "PageScans");

            migrationBuilder.DropColumn(
                name: "ProcessingTimeMs",
                table: "PageScans");

            migrationBuilder.DropColumn(
                name: "ResultJson",
                table: "PageScanDetails");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Browser",
                table: "PageScans",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "PageScans",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "PageScans",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceType",
                table: "PageScans",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatingSystem",
                table: "PageScans",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessingTimeMs",
                table: "PageScans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ResultJson",
                table: "PageScanDetails",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
