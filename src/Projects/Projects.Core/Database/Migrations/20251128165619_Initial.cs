using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Projects.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Projects");

            migrationBuilder.CreateTable(
                name: "Projects",
                schema: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    LastUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    JsonDocument = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetsDirectories",
                schema: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    DirectoryId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetsDirectories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetsDirectories_AssetsDirectories_DirectoryId",
                        column: x => x.DirectoryId,
                        principalSchema: "Projects",
                        principalTable: "AssetsDirectories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssetsDirectories_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "Projects",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetsFiles",
                schema: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SizeInBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    DirectoryId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetsFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetsFiles_AssetsDirectories_DirectoryId",
                        column: x => x.DirectoryId,
                        principalSchema: "Projects",
                        principalTable: "AssetsDirectories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AssetsFiles_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "Projects",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetsDirectories_DirectoryId",
                schema: "Projects",
                table: "AssetsDirectories",
                column: "DirectoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetsDirectories_ProjectId",
                schema: "Projects",
                table: "AssetsDirectories",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetsFiles_DirectoryId",
                schema: "Projects",
                table: "AssetsFiles",
                column: "DirectoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetsFiles_ProjectId",
                schema: "Projects",
                table: "AssetsFiles",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetsFiles",
                schema: "Projects");

            migrationBuilder.DropTable(
                name: "AssetsDirectories",
                schema: "Projects");

            migrationBuilder.DropTable(
                name: "Projects",
                schema: "Projects");
        }
    }
}
