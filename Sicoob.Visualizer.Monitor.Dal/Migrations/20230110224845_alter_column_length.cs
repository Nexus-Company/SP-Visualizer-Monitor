using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sicoob.Visualizer.Monitor.Dal.Migrations
{
    /// <inheritdoc />
    public partial class altercolumnlength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RefreshToken",
                table: "Authentications",
                type: "nvarchar(max)",
                maxLength: 2147483647,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2500)",
                oldMaxLength: 2500);

            migrationBuilder.AlterColumn<string>(
                name: "AccessToken",
                table: "Authentications",
                type: "nvarchar(max)",
                maxLength: 2147483647,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2500)",
                oldMaxLength: 2500);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RefreshToken",
                table: "Authentications",
                type: "nvarchar(2500)",
                maxLength: 2500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: 2147483647);

            migrationBuilder.AlterColumn<string>(
                name: "AccessToken",
                table: "Authentications",
                type: "nvarchar(2500)",
                maxLength: 2500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: 2147483647);
        }
    }
}
