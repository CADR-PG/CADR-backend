using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shop.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class Shop_v3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameVersion_Games_GameId",
                schema: "Shop",
                table: "GameVersion");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GameVersion",
                schema: "Shop",
                table: "GameVersion");

            migrationBuilder.RenameTable(
                name: "GameVersion",
                schema: "Shop",
                newName: "GameVersions",
                newSchema: "Shop");

            migrationBuilder.RenameIndex(
                name: "IX_GameVersion_GameId",
                schema: "Shop",
                table: "GameVersions",
                newName: "IX_GameVersions_GameId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GameVersions",
                schema: "Shop",
                table: "GameVersions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GameVersions_Games_GameId",
                schema: "Shop",
                table: "GameVersions",
                column: "GameId",
                principalSchema: "Shop",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameVersions_Games_GameId",
                schema: "Shop",
                table: "GameVersions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GameVersions",
                schema: "Shop",
                table: "GameVersions");

            migrationBuilder.RenameTable(
                name: "GameVersions",
                schema: "Shop",
                newName: "GameVersion",
                newSchema: "Shop");

            migrationBuilder.RenameIndex(
                name: "IX_GameVersions_GameId",
                schema: "Shop",
                table: "GameVersion",
                newName: "IX_GameVersion_GameId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GameVersion",
                schema: "Shop",
                table: "GameVersion",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GameVersion_Games_GameId",
                schema: "Shop",
                table: "GameVersion",
                column: "GameId",
                principalSchema: "Shop",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
