using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PulseWatch.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicStatusAndAdvancedCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomHeadersJson",
                table: "Websites",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HttpMethod",
                table: "Websites",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Websites",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ResponseBodyKeyword",
                table: "Websites",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomHeadersJson",
                table: "Websites");

            migrationBuilder.DropColumn(
                name: "HttpMethod",
                table: "Websites");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Websites");

            migrationBuilder.DropColumn(
                name: "ResponseBodyKeyword",
                table: "Websites");
        }
    }
}
