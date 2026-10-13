using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HighConvertAI.Server.Migrations
{
    /// <inheritdoc />
    public partial class addingPageScanUserID0 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PageScans_Users_UserId",
                table: "PageScans");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "PageScans",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

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
                table: "PageScans",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PageScans_Users_UserId",
                table: "PageScans",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PageScans_Users_UserId",
                table: "PageScans");

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
                table: "PageScans");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "PageScans",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PageScans_Users_UserId",
                table: "PageScans",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
