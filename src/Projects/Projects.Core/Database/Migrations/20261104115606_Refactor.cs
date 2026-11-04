using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Projects.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class Refactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "JsonDocument",
                schema: "Projects",
                table: "Projects",
                newName: "EditorMetaData");

            migrationBuilder.AddColumn<Guid>(
                name: "MainSceneFileId",
                schema: "Projects",
                table: "Projects",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MainSceneFileId",
                schema: "Projects",
                table: "Projects");

            migrationBuilder.RenameColumn(
                name: "EditorMetaData",
                schema: "Projects",
                table: "Projects",
                newName: "JsonDocument");
        }
    }
}
