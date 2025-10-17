using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiEdge.WarehouseService.Migrations
{
    /// <inheritdoc />
    public partial class affecteditems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemState_InventoryTransaction_RelatedTransactionId",
                table: "ItemState");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemState_Items_ItemId",
                table: "ItemState");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemState_Warehouses_WarehouseId",
                table: "ItemState");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ItemState",
                table: "ItemState");

            migrationBuilder.RenameTable(
                name: "ItemState",
                newName: "ItemStates");

            migrationBuilder.RenameIndex(
                name: "IX_ItemState_WarehouseId",
                table: "ItemStates",
                newName: "IX_ItemStates_WarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_ItemState_RelatedTransactionId",
                table: "ItemStates",
                newName: "IX_ItemStates_RelatedTransactionId");

            migrationBuilder.RenameIndex(
                name: "IX_ItemState_ItemId",
                table: "ItemStates",
                newName: "IX_ItemStates_ItemId");

            migrationBuilder.AddColumn<Guid>(
                name: "InventoryTransactionId",
                table: "Items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InventoryTransactionId",
                table: "ItemStates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ItemId1",
                table: "ItemStates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ItemStates",
                table: "ItemStates",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Items_InventoryTransactionId",
                table: "Items",
                column: "InventoryTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemStates_InventoryTransactionId",
                table: "ItemStates",
                column: "InventoryTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemStates_ItemId1",
                table: "ItemStates",
                column: "ItemId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_InventoryTransaction_InventoryTransactionId",
                table: "Items",
                column: "InventoryTransactionId",
                principalTable: "InventoryTransaction",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemStates_InventoryTransaction_InventoryTransactionId",
                table: "ItemStates",
                column: "InventoryTransactionId",
                principalTable: "InventoryTransaction",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemStates_InventoryTransaction_RelatedTransactionId",
                table: "ItemStates",
                column: "RelatedTransactionId",
                principalTable: "InventoryTransaction",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemStates_Items_ItemId",
                table: "ItemStates",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemStates_Items_ItemId1",
                table: "ItemStates",
                column: "ItemId1",
                principalTable: "Items",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemStates_Warehouses_WarehouseId",
                table: "ItemStates",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_InventoryTransaction_InventoryTransactionId",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemStates_InventoryTransaction_InventoryTransactionId",
                table: "ItemStates");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemStates_InventoryTransaction_RelatedTransactionId",
                table: "ItemStates");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemStates_Items_ItemId",
                table: "ItemStates");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemStates_Items_ItemId1",
                table: "ItemStates");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemStates_Warehouses_WarehouseId",
                table: "ItemStates");

            migrationBuilder.DropIndex(
                name: "IX_Items_InventoryTransactionId",
                table: "Items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ItemStates",
                table: "ItemStates");

            migrationBuilder.DropIndex(
                name: "IX_ItemStates_InventoryTransactionId",
                table: "ItemStates");

            migrationBuilder.DropIndex(
                name: "IX_ItemStates_ItemId1",
                table: "ItemStates");

            migrationBuilder.DropColumn(
                name: "InventoryTransactionId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "InventoryTransactionId",
                table: "ItemStates");

            migrationBuilder.DropColumn(
                name: "ItemId1",
                table: "ItemStates");

            migrationBuilder.RenameTable(
                name: "ItemStates",
                newName: "ItemState");

            migrationBuilder.RenameIndex(
                name: "IX_ItemStates_WarehouseId",
                table: "ItemState",
                newName: "IX_ItemState_WarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_ItemStates_RelatedTransactionId",
                table: "ItemState",
                newName: "IX_ItemState_RelatedTransactionId");

            migrationBuilder.RenameIndex(
                name: "IX_ItemStates_ItemId",
                table: "ItemState",
                newName: "IX_ItemState_ItemId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ItemState",
                table: "ItemState",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemState_InventoryTransaction_RelatedTransactionId",
                table: "ItemState",
                column: "RelatedTransactionId",
                principalTable: "InventoryTransaction",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemState_Items_ItemId",
                table: "ItemState",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemState_Warehouses_WarehouseId",
                table: "ItemState",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
