using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shop.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class Shop_v7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LibraryEntries_ShopUser_UserId",
                schema: "Shop",
                table: "LibraryEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ShopUser",
                schema: "Shop",
                table: "ShopUser");

            migrationBuilder.RenameTable(
                name: "ShopUser",
                schema: "Shop",
                newName: "ShopUsers",
                newSchema: "Shop");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ShopUsers",
                schema: "Shop",
                table: "ShopUsers",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LibraryEntries_ShopUsers_UserId",
                schema: "Shop",
                table: "LibraryEntries",
                column: "UserId",
                principalSchema: "Shop",
                principalTable: "ShopUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LibraryEntries_ShopUsers_UserId",
                schema: "Shop",
                table: "LibraryEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ShopUsers",
                schema: "Shop",
                table: "ShopUsers");

            migrationBuilder.RenameTable(
                name: "ShopUsers",
                schema: "Shop",
                newName: "ShopUser",
                newSchema: "Shop");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ShopUser",
                schema: "Shop",
                table: "ShopUser",
                column: "Id");

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
    }
}
