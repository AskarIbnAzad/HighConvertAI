using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HighConvertAI.Server.Migrations
{
    /// <inheritdoc />
    public partial class fixingrealations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PageScanDetails_PageScanId",
                table: "PageScanDetails");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Can perform audits", "User" });

            migrationBuilder.CreateIndex(
                name: "IX_PageScanDetails_PageScanId",
                table: "PageScanDetails",
                column: "PageScanId",
                unique: true,
                filter: "[PageScanId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PageScanDetails_PageScanId",
                table: "PageScanDetails");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Can create and manage CRO audits", "Auditor" });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[] { 3, "Read-only access to audit reports", "Viewer" });

            migrationBuilder.CreateIndex(
                name: "IX_PageScanDetails_PageScanId",
                table: "PageScanDetails",
                column: "PageScanId");
        }
    }
}
