using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SP.Visualizer.Monitor.Dal.Migrations
{
    /// <inheritdoc />
    public partial class addfolder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(449)", maxLength: 449, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sites",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(449)", maxLength: 449, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WebUrl = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Authentications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TokenType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(max)", maxLength: 2147483647, nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", maxLength: 2147483647, nullable: false),
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
                name: "Lists",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(449)", maxLength: 449, nullable: false),
                    SiteId = table.Column<string>(type: "nvarchar(449)", maxLength: 449, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lists_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Folders",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ListId = table.Column<string>(type: "nvarchar(449)", nullable: false),
                    Directory = table.Column<string>(type: "nvarchar(max)", maxLength: 2147483647, nullable: false),
                    FatherId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Folders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Folders_Folders_FatherId",
                        column: x => x.FatherId,
                        principalTable: "Folders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Folders_Lists_ListId",
                        column: x => x.ListId,
                        principalTable: "Lists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    WebUrl = table.Column<string>(type: "nvarchar(max)", maxLength: 2147483647, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(449)", maxLength: 449, nullable: false),
                    FolderId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ListId = table.Column<string>(type: "nvarchar(449)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Items_Folders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "Folders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Items_Lists_ListId",
                        column: x => x.ListId,
                        principalTable: "Lists",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    User = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Target = table.Column<string>(type: "nvarchar(320)", nullable: false),
                    Type = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
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
                name: "IX_Accounts_Id_Name",
                table: "Accounts",
                columns: new[] { "Id", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_Target_User_Date",
                table: "Activities",
                columns: new[] { "Target", "User", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_User",
                table: "Activities",
                column: "User");

            migrationBuilder.CreateIndex(
                name: "IX_Authentications_Account",
                table: "Authentications",
                column: "Account");

            migrationBuilder.CreateIndex(
                name: "IX_Folders_FatherId",
                table: "Folders",
                column: "FatherId");

            migrationBuilder.CreateIndex(
                name: "IX_Folders_ListId",
                table: "Folders",
                column: "ListId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_FolderId",
                table: "Items",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Id_Name",
                table: "Items",
                columns: new[] { "Id", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_ListId",
                table: "Items",
                column: "ListId");

            migrationBuilder.CreateIndex(
                name: "IX_Lists_SiteId",
                table: "Lists",
                column: "SiteId");
        }

        /// <inheritdoc />
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

            migrationBuilder.DropTable(
                name: "Folders");

            migrationBuilder.DropTable(
                name: "Lists");

            migrationBuilder.DropTable(
                name: "Sites");
        }
    }
}
