using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Projects.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class Final : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "FileSize",
                schema: "Projects",
                table: "Assets",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileSize",
                schema: "Projects",
                table: "Assets");
        }
    }
}
