using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Users.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class EmailConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmailConfirmation_Code",
                schema: "Users",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmation_ExpiresAt",
                schema: "Users",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmation_IsConfirmed",
                schema: "Users",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmation_SentAt",
                schema: "Users",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailConfirmation_Code",
                schema: "Users",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailConfirmation_ExpiresAt",
                schema: "Users",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailConfirmation_IsConfirmed",
                schema: "Users",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailConfirmation_SentAt",
                schema: "Users",
                table: "Users");
        }
    }
}
