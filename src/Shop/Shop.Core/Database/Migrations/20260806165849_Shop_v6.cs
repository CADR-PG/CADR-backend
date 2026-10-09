using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shop.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class Shop_v6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LibraryEntries_Order_OrderId",
                schema: "Shop",
                table: "LibraryEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Order",
                schema: "Shop",
                table: "Order");

            migrationBuilder.RenameTable(
                name: "Order",
                schema: "Shop",
                newName: "Orders",
                newSchema: "Shop");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Orders",
                schema: "Shop",
                table: "Orders",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LibraryEntries_Orders_OrderId",
                schema: "Shop",
                table: "LibraryEntries",
                column: "OrderId",
                principalSchema: "Shop",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LibraryEntries_Orders_OrderId",
                schema: "Shop",
                table: "LibraryEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Orders",
                schema: "Shop",
                table: "Orders");

            migrationBuilder.RenameTable(
                name: "Orders",
                schema: "Shop",
                newName: "Order",
                newSchema: "Shop");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Order",
                schema: "Shop",
                table: "Order",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LibraryEntries_Order_OrderId",
                schema: "Shop",
                table: "LibraryEntries",
                column: "OrderId",
                principalSchema: "Shop",
                principalTable: "Order",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
