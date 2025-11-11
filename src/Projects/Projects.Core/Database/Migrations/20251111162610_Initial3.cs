using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Projects.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class Initial3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BlobName",
                schema: "Projects",
                table: "Assets",
                newName: "BlobPath");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BlobPath",
                schema: "Projects",
                table: "Assets",
                newName: "BlobName");
        }
    }
}
