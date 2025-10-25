using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiEdge.WarehouseService.Migrations
{
    /// <inheritdoc />
    public partial class OutboundTransactionAddDraftItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OutboundTransactionId",
                table: "Items",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_OutboundTransactionId",
                table: "Items",
                column: "OutboundTransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_OutboundTransactions_OutboundTransactionId",
                table: "Items",
                column: "OutboundTransactionId",
                principalTable: "OutboundTransactions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_OutboundTransactions_OutboundTransactionId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_OutboundTransactionId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "OutboundTransactionId",
                table: "Items");
        }
    }
}
