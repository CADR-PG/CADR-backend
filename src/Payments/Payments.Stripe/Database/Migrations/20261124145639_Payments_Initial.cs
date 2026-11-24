using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payments.Stripe.Database.Migrations
{
    /// <inheritdoc />
    public partial class Payments_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Payments-Stripe");

            migrationBuilder.CreateTable(
                name: "StripeCustomers",
                schema: "Payments-Stripe",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StripeCustomerId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StripeCustomers", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StripeCustomers",
                schema: "Payments-Stripe");
        }
    }
}
