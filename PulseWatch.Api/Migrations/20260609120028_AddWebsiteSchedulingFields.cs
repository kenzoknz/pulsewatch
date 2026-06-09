using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PulseWatch.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWebsiteSchedulingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CheckIntervalMinutes",
                table: "Websites",
                newName: "CheckIntervalSeconds");

            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "Websites",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCheckedAt",
                table: "Websites",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LastResponseTimeMs",
                table: "Websites",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastStatusCode",
                table: "Websites",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextCheckAt",
                table: "Websites",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            // Migrate data: chuyển CheckIntervalMinutes (đã bị rename thành CheckIntervalSeconds) sang giây
            migrationBuilder.Sql("UPDATE Websites SET CheckIntervalSeconds = CheckIntervalSeconds * 60");
            
            // Set NextCheckAt cho các record hiện tại
            migrationBuilder.Sql("UPDATE Websites SET NextCheckAt = GETUTCDATE()");

            migrationBuilder.CreateIndex(
                name: "IX_Websites_IsActive_NextCheckAt",
                table: "Websites",
                columns: new[] { "IsActive", "NextCheckAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Websites_IsActive_NextCheckAt",
                table: "Websites");

            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "Websites");

            migrationBuilder.DropColumn(
                name: "LastCheckedAt",
                table: "Websites");

            migrationBuilder.DropColumn(
                name: "LastResponseTimeMs",
                table: "Websites");

            migrationBuilder.DropColumn(
                name: "LastStatusCode",
                table: "Websites");

            migrationBuilder.DropColumn(
                name: "NextCheckAt",
                table: "Websites");

            // Revert data
            migrationBuilder.Sql("UPDATE Websites SET CheckIntervalSeconds = CheckIntervalSeconds / 60");

            migrationBuilder.RenameColumn(
                name: "CheckIntervalSeconds",
                table: "Websites",
                newName: "CheckIntervalMinutes");
        }
    }
}
