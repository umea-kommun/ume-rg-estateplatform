using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umea.se.EstateService.DataStore.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrderStatusRefresh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PythagorasStatusCategory",
                table: "WorkOrders",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StatusCheckedAt",
                table: "WorkOrders",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PythagorasStatusCategory",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "StatusCheckedAt",
                table: "WorkOrders");
        }
    }
}
