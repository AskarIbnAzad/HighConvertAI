using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HighConvertAI.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddingIssueTablesWithOldTableRemoved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExtractedSignals");

            migrationBuilder.DropTable(
                name: "ScanRuleConditionResults");

            migrationBuilder.DropTable(
                name: "RuleConditions");

            migrationBuilder.DropTable(
                name: "ScanRuleResults");

            migrationBuilder.DropTable(
                name: "Rules");

            migrationBuilder.DropIndex(
                name: "IX_PageScans_CreatedAtUtc",
                table: "PageScans");

            migrationBuilder.DropIndex(
                name: "IX_PageScans_PageType",
                table: "PageScans");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "PageScans",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "PageType",
                table: "PageScans",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateTable(
                name: "IssueGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortingOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IssueSeveritys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AffectedScore = table.Column<int>(type: "int", nullable: false),
                    AffectedRevinueOpportunity = table.Column<int>(type: "int", nullable: false),
                    SortingOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueSeveritys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PageScanDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PageScanId = table.Column<int>(type: "int", nullable: true),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Browser = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DeviceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OperatingSystem = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProcessingTimeMs = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageScanDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PageScanDetails_PageScans_PageScanId",
                        column: x => x.PageScanId,
                        principalTable: "PageScans",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Issues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PageScanId = table.Column<int>(type: "int", nullable: true),
                    IssueGroupId = table.Column<int>(type: "int", nullable: true),
                    IssueSeverityId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WhatWasRight = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WhatToImprove = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reccomendation = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Issues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Issues_IssueGroups_IssueGroupId",
                        column: x => x.IssueGroupId,
                        principalTable: "IssueGroups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Issues_IssueSeveritys_IssueSeverityId",
                        column: x => x.IssueSeverityId,
                        principalTable: "IssueSeveritys",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Issues_PageScans_PageScanId",
                        column: x => x.PageScanId,
                        principalTable: "PageScans",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Issues_IssueGroupId",
                table: "Issues",
                column: "IssueGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_IssueSeverityId",
                table: "Issues",
                column: "IssueSeverityId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_PageScanId",
                table: "Issues",
                column: "PageScanId");

            migrationBuilder.CreateIndex(
                name: "IX_PageScanDetails_PageScanId",
                table: "PageScanDetails",
                column: "PageScanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Issues");

            migrationBuilder.DropTable(
                name: "PageScanDetails");

            migrationBuilder.DropTable(
                name: "IssueGroups");

            migrationBuilder.DropTable(
                name: "IssueSeveritys");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "PageScans",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "PageType",
                table: "PageScans",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "ExtractedSignals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PageScanId = table.Column<int>(type: "int", nullable: false),
                    SignalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SignalValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtractedSignals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExtractedSignals_PageScans_PageScanId",
                        column: x => x.PageScanId,
                        principalTable: "PageScans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Importance = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MinimumPassCount = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    PageType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RuleType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ScoringCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuleConditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleId = table.Column<int>(type: "int", nullable: false),
                    ExpectedValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    Operator = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SignalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleConditions_Rules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "Rules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScanRuleResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PageScanId = table.Column<int>(type: "int", nullable: false),
                    RuleId = table.Column<int>(type: "int", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Passed = table.Column<bool>(type: "bit", nullable: false),
                    PassedConditionCount = table.Column<int>(type: "int", nullable: false),
                    ScoreAwarded = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TotalConditionCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanRuleResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScanRuleResults_PageScans_PageScanId",
                        column: x => x.PageScanId,
                        principalTable: "PageScans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScanRuleResults_Rules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "Rules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScanRuleConditionResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleConditionId = table.Column<int>(type: "int", nullable: false),
                    ScanRuleResultId = table.Column<int>(type: "int", nullable: false),
                    ActualValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpectedValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    Operator = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Passed = table.Column<bool>(type: "bit", nullable: false),
                    SignalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanRuleConditionResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScanRuleConditionResults_RuleConditions_RuleConditionId",
                        column: x => x.RuleConditionId,
                        principalTable: "RuleConditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScanRuleConditionResults_ScanRuleResults_ScanRuleResultId",
                        column: x => x.ScanRuleResultId,
                        principalTable: "ScanRuleResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PageScans_CreatedAtUtc",
                table: "PageScans",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PageScans_PageType",
                table: "PageScans",
                column: "PageType");

            migrationBuilder.CreateIndex(
                name: "IX_ExtractedSignals_PageScanId",
                table: "ExtractedSignals",
                column: "PageScanId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtractedSignals_PageScanId_SignalName",
                table: "ExtractedSignals",
                columns: new[] { "PageScanId", "SignalName" });

            migrationBuilder.CreateIndex(
                name: "IX_RuleConditions_RuleId",
                table: "RuleConditions",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleConditions_RuleId_SortOrder",
                table: "RuleConditions",
                columns: new[] { "RuleId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Rules_Code",
                table: "Rules",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rules_PageType_IsActive",
                table: "Rules",
                columns: new[] { "PageType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ScanRuleConditionResults_RuleConditionId",
                table: "ScanRuleConditionResults",
                column: "RuleConditionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScanRuleConditionResults_ScanRuleResultId",
                table: "ScanRuleConditionResults",
                column: "ScanRuleResultId");

            migrationBuilder.CreateIndex(
                name: "IX_ScanRuleResults_PageScanId",
                table: "ScanRuleResults",
                column: "PageScanId");

            migrationBuilder.CreateIndex(
                name: "IX_ScanRuleResults_PageScanId_RuleId",
                table: "ScanRuleResults",
                columns: new[] { "PageScanId", "RuleId" });

            migrationBuilder.CreateIndex(
                name: "IX_ScanRuleResults_RuleId",
                table: "ScanRuleResults",
                column: "RuleId");
        }
    }
}
