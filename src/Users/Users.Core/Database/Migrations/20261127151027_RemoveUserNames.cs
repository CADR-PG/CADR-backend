using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Users.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // One-off data migration: names move to the Community module. Preserve them for users who do not have
            // a community profile yet (existing profiles keep their data). Skipped when the Community schema does not exist
            // yet (fresh database) - missing profiles are then created by the daily synchronization.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF to_regclass('"Community"."UserProfiles"') IS NOT NULL THEN
                        INSERT INTO "Community"."UserProfiles" ("UserId", "FirstName", "LastName", "Name", "IsNameCustomized")
                        SELECT u."Id",
                               left(btrim(u."FirstName"), 100),
                               left(btrim(u."LastName"), 100),
                               left(coalesce(nullif(btrim(concat_ws(' ', btrim(u."FirstName"), btrim(u."LastName"))), ''), split_part(u."Email", '@', 1)), 100),
                               FALSE
                        FROM "Users"."Users" AS u
                        ON CONFLICT ("UserId") DO NOTHING;
                    END IF;
                END $$;
                """);

            migrationBuilder.DropColumn(
                name: "FirstName",
                schema: "Users",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastName",
                schema: "Users",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                schema: "Users",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                schema: "Users",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
