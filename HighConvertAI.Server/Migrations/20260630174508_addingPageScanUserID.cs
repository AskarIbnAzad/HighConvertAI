using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HighConvertAI.Server.Migrations
{
    /// <inheritdoc />
    public partial class addingPageScanUserID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "PageScans",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_PageScans_UserId",
                table: "PageScans",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PageScans_Users_UserId",
                table: "PageScans",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PageScans_Users_UserId",
                table: "PageScans");

            migrationBuilder.DropIndex(
                name: "IX_PageScans_UserId",
                table: "PageScans");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PageScans");
        }
    }
}
