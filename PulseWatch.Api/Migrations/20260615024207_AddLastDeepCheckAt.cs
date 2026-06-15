using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PulseWatch.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLastDeepCheckAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastDeepCheckAt",
                table: "Websites",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastDeepCheckAt",
                table: "Websites");
        }
    }
}
