using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sicoob.Visualizer.Monitor.Dal.Migrations
{
    public partial class add_activity_in_context : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    WebUrl = table.Column<string>(type: "nvarchar(max)", maxLength: 2147483647, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Authentications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TokenType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(2500)", maxLength: 2500, nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(2500)", maxLength: 2500, nullable: false),
                    Account = table.Column<string>(type: "nvarchar(320)", nullable: false),
                    RefreshIn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authentications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Authentications_Accounts_Account",
                        column: x => x.Account,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Target = table.Column<string>(type: "nvarchar(320)", nullable: false),
                    Type = table.Column<byte>(type: "tinyint", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    User = table.Column<string>(type: "nvarchar(320)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => new { x.Id, x.Target, x.Type });
                    table.ForeignKey(
                        name: "FK_Activities_Accounts_User",
                        column: x => x.User,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Activities_Items_Target",
                        column: x => x.Target,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_Target",
                table: "Activities",
                column: "Target");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_User",
                table: "Activities",
                column: "User");

            migrationBuilder.CreateIndex(
                name: "IX_Authentications_Account",
                table: "Authentications",
                column: "Account");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "Authentications");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
