using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sicoob.Visualizer.Monitor.Dal.Migrations
{
    public partial class add_item_Name : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Items",
                type: "nvarchar(max)",
                maxLength: 2147483647,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Items");
        }
    }
}
