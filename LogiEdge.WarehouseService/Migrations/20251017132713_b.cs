using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiEdge.WarehouseService.Migrations
{
    /// <inheritdoc />
    public partial class b : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_InventoryTransaction_InventoryTransactionId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_InventoryTransactionId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "InventoryTransactionId",
                table: "Items");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InventoryTransactionId",
                table: "Items",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_InventoryTransactionId",
                table: "Items",
                column: "InventoryTransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_InventoryTransaction_InventoryTransactionId",
                table: "Items",
                column: "InventoryTransactionId",
                principalTable: "InventoryTransaction",
                principalColumn: "Id");
        }
    }
}
