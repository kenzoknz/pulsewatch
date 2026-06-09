using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PulseWatch.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexUptimeChecksWebsiteCheckedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UptimeChecks_WebsiteId",
                table: "UptimeChecks");

            migrationBuilder.CreateIndex(
                name: "IX_UptimeChecks_WebsiteId_CheckedAt",
                table: "UptimeChecks",
                columns: new[] { "WebsiteId", "CheckedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UptimeChecks_WebsiteId_CheckedAt",
                table: "UptimeChecks");

            migrationBuilder.CreateIndex(
                name: "IX_UptimeChecks_WebsiteId",
                table: "UptimeChecks",
                column: "WebsiteId");
        }
    }
}
