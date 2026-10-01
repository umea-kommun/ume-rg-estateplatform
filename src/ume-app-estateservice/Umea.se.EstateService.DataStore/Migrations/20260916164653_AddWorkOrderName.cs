using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umea.se.EstateService.DataStore.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrderName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PythagorasWorkOrderName",
                table: "WorkOrders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PythagorasWorkOrderName",
                table: "WorkOrders");
        }
    }
}
