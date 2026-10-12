using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shop.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class Shop_v8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Wallets",
                schema: "Shop",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ballance = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wallets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wallets_ShopUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "Shop",
                        principalTable: "ShopUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_UserId",
                schema: "Shop",
                table: "Wallets",
                column: "UserId",
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO "Shop"."Wallets" ("Id", "UserId", "Ballance")
                SELECT gen_random_uuid(), "Id", 0
                FROM "Shop"."ShopUsers"
                WHERE NOT EXISTS (
                    SELECT 1 FROM "Shop"."Wallets" WHERE "Wallets"."UserId" = "ShopUsers"."Id"
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Wallets",
                schema: "Shop");
        }
    }
}
