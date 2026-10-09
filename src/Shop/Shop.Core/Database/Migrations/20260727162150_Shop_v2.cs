using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shop.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class Shop_v2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GameId",
                schema: "Shop",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                schema: "Shop",
                table: "Games");

            migrationBuilder.AddColumn<int>(
                name: "AgeRestriction",
                schema: "Shop",
                table: "Games",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "State",
                schema: "Shop",
                table: "Games",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "GameVersion",
                schema: "Shop",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameVersion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameVersion_Games_GameId",
                        column: x => x.GameId,
                        principalSchema: "Shop",
                        principalTable: "Games",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShopUser",
                schema: "Shop",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopUser", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameVersion_GameId",
                schema: "Shop",
                table: "GameVersion",
                column: "GameId");

            migrationBuilder.AddForeignKey(
                name: "FK_LibraryEntries_ShopUser_UserId",
                schema: "Shop",
                table: "LibraryEntries",
                column: "UserId",
                principalSchema: "Shop",
                principalTable: "ShopUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LibraryEntries_ShopUser_UserId",
                schema: "Shop",
                table: "LibraryEntries");

            migrationBuilder.DropTable(
                name: "GameVersion",
                schema: "Shop");

            migrationBuilder.DropTable(
                name: "ShopUser",
                schema: "Shop");

            migrationBuilder.DropColumn(
                name: "AgeRestriction",
                schema: "Shop",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "State",
                schema: "Shop",
                table: "Games");

            migrationBuilder.AddColumn<Guid>(
                name: "GameId",
                schema: "Shop",
                table: "Games",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                schema: "Shop",
                table: "Games",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
