using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SP.Visualizer.Monitor.Dal.Migrations
{
    /// <inheritdoc />
    public partial class alterindexs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Folders_Name_ListId",
                table: "Folders");

            migrationBuilder.DropIndex(
                name: "IX_Activities_Target_User_Date",
                table: "Activities");

            migrationBuilder.CreateIndex(
                name: "IX_Lists_Id",
                table: "Lists",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Folders_Id_Name_ListId",
                table: "Folders",
                columns: new[] { "Id", "Name", "ListId" });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_Target_User_Date_Inserted",
                table: "Activities",
                columns: new[] { "Target", "User", "Date", "Inserted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lists_Id",
                table: "Lists");

            migrationBuilder.DropIndex(
                name: "IX_Folders_Id_Name_ListId",
                table: "Folders");

            migrationBuilder.DropIndex(
                name: "IX_Activities_Target_User_Date_Inserted",
                table: "Activities");

            migrationBuilder.CreateIndex(
                name: "IX_Folders_Name_ListId",
                table: "Folders",
                columns: new[] { "Name", "ListId" });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_Target_User_Date",
                table: "Activities",
                columns: new[] { "Target", "User", "Date" });
        }
    }
}
