using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Users.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "JsonDocument",
                schema: "Users",
                table: "Projects",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "json");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdate",
                schema: "Users",
                table: "Projects",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUpdate",
                schema: "Users",
                table: "Projects");

            migrationBuilder.AlterColumn<string>(
                name: "JsonDocument",
                schema: "Users",
                table: "Projects",
                type: "json",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
