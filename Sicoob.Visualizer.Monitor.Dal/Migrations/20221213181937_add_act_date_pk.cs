using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sicoob.Visualizer.Monitor.Dal.Migrations
{
    public partial class add_act_date_pk : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Activities",
                table: "Activities");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Activities",
                table: "Activities",
                columns: new[] { "Id", "Target", "Type", "Date" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Activities",
                table: "Activities");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Activities",
                table: "Activities",
                columns: new[] { "Id", "Target", "Type" });
        }
    }
}
