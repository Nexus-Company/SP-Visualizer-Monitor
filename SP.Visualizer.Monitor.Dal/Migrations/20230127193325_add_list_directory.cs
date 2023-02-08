using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SP.Visualizer.Monitor.Dal.Migrations
{
    /// <inheritdoc />
    public partial class addlistdirectory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Directory",
                table: "Lists",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WebUrl",
                table: "Lists",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "FolderId",
                table: "Items",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(449)",
                oldMaxLength: 449,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Directory",
                table: "Lists");

            migrationBuilder.DropColumn(
                name: "WebUrl",
                table: "Lists");

            migrationBuilder.AlterColumn<string>(
                name: "FolderId",
                table: "Items",
                type: "nvarchar(449)",
                maxLength: 449,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);
        }
    }
}
