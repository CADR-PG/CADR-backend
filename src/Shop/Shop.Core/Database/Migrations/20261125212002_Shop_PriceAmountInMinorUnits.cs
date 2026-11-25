using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shop.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class Shop_PriceAmountInMinorUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // decimal (złotówki) -> bigint (grosze): 19.99 -> 1999
            migrationBuilder.Sql(
                """
                ALTER TABLE "Shop"."Prices"
                ALTER COLUMN "Amount" TYPE bigint
                USING ROUND("Amount" * 100);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // bigint (grosze) -> decimal (złotówki): 1999 -> 19.99
            migrationBuilder.Sql(
                """
                ALTER TABLE "Shop"."Prices"
                ALTER COLUMN "Amount" TYPE numeric
                USING "Amount" / 100.0;
                """);
        }
    }
}
