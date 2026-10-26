using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiEdge.CustomerService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Abbreviation = table.Column<string>(type: "text", nullable: false),
                    CanonicalName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ShortName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Addresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Street = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    HouseNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ZipCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    City = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Country = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsDefaultDeliveryAddress = table.Column<bool>(type: "boolean", nullable: false),
                    BillingCustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeliveryCustomerId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.Id);
                    table.CheckConstraint("CK_Addresses_SingleRole", "\"BillingCustomerId\" IS NULL OR \"DeliveryCustomerId\" IS NULL");
                    table.ForeignKey(
                        name: "FK_Addresses_Customers_BillingCustomerId",
                        column: x => x.BillingCustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Addresses_Customers_DeliveryCustomerId",
                        column: x => x.DeliveryCustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_BillingCustomerId",
                table: "Addresses",
                column: "BillingCustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_DeliveryCustomerId",
                table: "Addresses",
                column: "DeliveryCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Abbreviation",
                table: "Customers",
                column: "Abbreviation",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Addresses");

            migrationBuilder.DropTable(
                name: "Customers");
        }
    }
}
