using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseFlow.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseOccurredAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 案件實際發生時間（補件用）。可為 null；未填時應用程式會以建立時間帶入。
            migrationBuilder.AddColumn<DateTime>(
                name: "occurred_at",
                table: "cases",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "occurred_at",
                table: "cases");
        }
    }
}
