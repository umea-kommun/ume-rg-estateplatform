using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umea.se.EstateService.DataStore.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrderStatusChangedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StatusChangedAt",
                table: "WorkOrders",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StatusChangedAt",
                table: "WorkOrders");
        }
    }
}
