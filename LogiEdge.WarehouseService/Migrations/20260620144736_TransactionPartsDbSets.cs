using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiEdge.WarehouseService.Migrations
{
    /// <inheritdoc />
    public partial class TransactionPartsDbSets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InboundDraftItem_InboundTransactionPart_InboundTransactionP~",
                table: "InboundDraftItem");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_InboundTransactionPart_InboundTransac~",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_OutboundTransactionPart_OutboundTrans~",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_RelocationTransactionPart_RelocationT~",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_OutboundTransactionPart_OutboundTransactionPartId",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemState_InboundTransactionPart_InboundTransactionPartId",
                table: "ItemState");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemState_OutboundTransactionPart_OutboundTransactionPartId",
                table: "ItemState");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemState_RelocationTransactionPart_RelocationTransactionPa~",
                table: "ItemState");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RelocationTransactionPart",
                table: "RelocationTransactionPart");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OutboundTransactionPart",
                table: "OutboundTransactionPart");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InboundTransactionPart",
                table: "InboundTransactionPart");

            migrationBuilder.RenameTable(
                name: "RelocationTransactionPart",
                newName: "RelocationTransactions");

            migrationBuilder.RenameTable(
                name: "OutboundTransactionPart",
                newName: "OutboundTransactionParts");

            migrationBuilder.RenameTable(
                name: "InboundTransactionPart",
                newName: "InboundTransactionParts");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RelocationTransactions",
                table: "RelocationTransactions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OutboundTransactionParts",
                table: "OutboundTransactionParts",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InboundTransactionParts",
                table: "InboundTransactionParts",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InboundDraftItem_InboundTransactionParts_InboundTransaction~",
                table: "InboundDraftItem",
                column: "InboundTransactionPartId",
                principalTable: "InboundTransactionParts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_InboundTransactionParts_InboundTransa~",
                table: "InventoryTransactions",
                column: "InboundTransactionPartId",
                principalTable: "InboundTransactionParts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_OutboundTransactionParts_OutboundTran~",
                table: "InventoryTransactions",
                column: "OutboundTransactionPartId",
                principalTable: "OutboundTransactionParts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_RelocationTransactions_RelocationTran~",
                table: "InventoryTransactions",
                column: "RelocationTransactionPartId",
                principalTable: "RelocationTransactions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_OutboundTransactionParts_OutboundTransactionPartId",
                table: "Items",
                column: "OutboundTransactionPartId",
                principalTable: "OutboundTransactionParts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemState_InboundTransactionParts_InboundTransactionPartId",
                table: "ItemState",
                column: "InboundTransactionPartId",
                principalTable: "InboundTransactionParts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemState_OutboundTransactionParts_OutboundTransactionPartId",
                table: "ItemState",
                column: "OutboundTransactionPartId",
                principalTable: "OutboundTransactionParts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemState_RelocationTransactions_RelocationTransactionPartId",
                table: "ItemState",
                column: "RelocationTransactionPartId",
                principalTable: "RelocationTransactions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InboundDraftItem_InboundTransactionParts_InboundTransaction~",
                table: "InboundDraftItem");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_InboundTransactionParts_InboundTransa~",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_OutboundTransactionParts_OutboundTran~",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_RelocationTransactions_RelocationTran~",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_OutboundTransactionParts_OutboundTransactionPartId",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemState_InboundTransactionParts_InboundTransactionPartId",
                table: "ItemState");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemState_OutboundTransactionParts_OutboundTransactionPartId",
                table: "ItemState");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemState_RelocationTransactions_RelocationTransactionPartId",
                table: "ItemState");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RelocationTransactions",
                table: "RelocationTransactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OutboundTransactionParts",
                table: "OutboundTransactionParts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InboundTransactionParts",
                table: "InboundTransactionParts");

            migrationBuilder.RenameTable(
                name: "RelocationTransactions",
                newName: "RelocationTransactionPart");

            migrationBuilder.RenameTable(
                name: "OutboundTransactionParts",
                newName: "OutboundTransactionPart");

            migrationBuilder.RenameTable(
                name: "InboundTransactionParts",
                newName: "InboundTransactionPart");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RelocationTransactionPart",
                table: "RelocationTransactionPart",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OutboundTransactionPart",
                table: "OutboundTransactionPart",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InboundTransactionPart",
                table: "InboundTransactionPart",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InboundDraftItem_InboundTransactionPart_InboundTransactionP~",
                table: "InboundDraftItem",
                column: "InboundTransactionPartId",
                principalTable: "InboundTransactionPart",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_InboundTransactionPart_InboundTransac~",
                table: "InventoryTransactions",
                column: "InboundTransactionPartId",
                principalTable: "InboundTransactionPart",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_OutboundTransactionPart_OutboundTrans~",
                table: "InventoryTransactions",
                column: "OutboundTransactionPartId",
                principalTable: "OutboundTransactionPart",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_RelocationTransactionPart_RelocationT~",
                table: "InventoryTransactions",
                column: "RelocationTransactionPartId",
                principalTable: "RelocationTransactionPart",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_OutboundTransactionPart_OutboundTransactionPartId",
                table: "Items",
                column: "OutboundTransactionPartId",
                principalTable: "OutboundTransactionPart",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemState_InboundTransactionPart_InboundTransactionPartId",
                table: "ItemState",
                column: "InboundTransactionPartId",
                principalTable: "InboundTransactionPart",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemState_OutboundTransactionPart_OutboundTransactionPartId",
                table: "ItemState",
                column: "OutboundTransactionPartId",
                principalTable: "OutboundTransactionPart",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemState_RelocationTransactionPart_RelocationTransactionPa~",
                table: "ItemState",
                column: "RelocationTransactionPartId",
                principalTable: "RelocationTransactionPart",
                principalColumn: "Id");
        }
    }
}
