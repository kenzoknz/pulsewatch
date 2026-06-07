using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PulseWatch.Api.Migrations
{
    /// <inheritdoc />
    public partial class Downtime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "DowntimeEvents",
                newName: "StartedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StartedAt",
                table: "DowntimeEvents",
                newName: "StartTime");
        }
    }
}
