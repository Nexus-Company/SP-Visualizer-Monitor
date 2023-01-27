using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sicoob.Visualizer.Monitor.Dal.Migrations
{
    /// <inheritdoc />
    public partial class addactivityinserted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_Id_Name_Etag",
                table: "Items");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Folders",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "Inserted",
                table: "Activities",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Items_Id_Name_FolderId_ListId",
                table: "Items",
                columns: new[] { "Id", "Name", "FolderId", "ListId" });

            migrationBuilder.CreateIndex(
                name: "IX_Folders_Name_ListId",
                table: "Folders",
                columns: new[] { "Name", "ListId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_Id_Name_FolderId_ListId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Folders_Name_ListId",
                table: "Folders");

            migrationBuilder.DropColumn(
                name: "Inserted",
                table: "Activities");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Folders",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Id_Name_Etag",
                table: "Items",
                columns: new[] { "Id", "Name", "Etag" });
        }
    }
}
