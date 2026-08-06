using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shop.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class Shop_v5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActiveVersionId",
                schema: "Shop",
                table: "Games",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveVersionId",
                schema: "Shop",
                table: "Games");
        }
    }
}
