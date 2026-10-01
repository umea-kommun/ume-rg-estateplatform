using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umea.se.EstateService.DataStore.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrderPerformedDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAt",
                table: "WorkOrders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PerformedDescription",
                table: "WorkOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PerformedDescriptionAt",
                table: "WorkOrders",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "PerformedDescription",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "PerformedDescriptionAt",
                table: "WorkOrders");
        }
    }
}
