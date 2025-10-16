using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiEdge.WarehouseService.Migrations
{
    /// <inheritdoc />
    public partial class _2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransaction_Warehouses_WarehouseId",
                table: "InventoryTransaction");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransaction_Warehouses_WarehouseId",
                table: "InventoryTransaction",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransaction_Warehouses_WarehouseId",
                table: "InventoryTransaction");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransaction_Warehouses_WarehouseId",
                table: "InventoryTransaction",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
