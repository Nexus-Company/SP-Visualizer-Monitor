using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SP.Visualizer.Monitor.Dal.Migrations
{
    /// <inheritdoc />
    public partial class addlistdriveid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_Id_Name",
                table: "Items");

            migrationBuilder.AddColumn<string>(
                name: "DriveId",
                table: "Lists",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Etag",
                table: "Items",
                type: "nvarchar(449)",
                maxLength: 449,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MimeType",
                table: "Items",
                type: "nvarchar(max)",
                maxLength: 2147483647,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Id_Name_Etag",
                table: "Items",
                columns: new[] { "Id", "Name", "Etag" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_Id_Name_Etag",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "DriveId",
                table: "Lists");

            migrationBuilder.DropColumn(
                name: "Etag",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "MimeType",
                table: "Items");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Id_Name",
                table: "Items",
                columns: new[] { "Id", "Name" });
        }
    }
}
