using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseFlow.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "google_sub",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "google_email",
                table: "users",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "auth_provider",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValueSql: "'local'::character varying");

            migrationBuilder.CreateIndex(
                name: "idx_users_google_sub",
                table: "users",
                column: "google_sub",
                unique: true,
                filter: "google_sub IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_users_google_sub",
                table: "users");

            migrationBuilder.DropColumn(
                name: "google_sub",
                table: "users");

            migrationBuilder.DropColumn(
                name: "google_email",
                table: "users");

            migrationBuilder.DropColumn(
                name: "auth_provider",
                table: "users");
        }
    }
}
