using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiEdge.WarehouseService.Migrations
{
    /// <inheritdoc />
    public partial class AddBookedDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Date",
                table: "InventoryTransactions",
                newName: "CreatedDate");

            migrationBuilder.AddColumn<DateTime>(
                name: "BookedDate",
                table: "InventoryTransactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"InventoryTransactions\" SET \"BookedDate\" = '2026-06-04T00:00:00Z' WHERE \"State\" = 1 AND \"BookedDate\" IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookedDate",
                table: "InventoryTransactions");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "InventoryTransactions",
                newName: "Date");
        }
    }
}
