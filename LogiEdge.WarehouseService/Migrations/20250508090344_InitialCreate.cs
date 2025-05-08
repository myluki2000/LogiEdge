using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiEdge.WarehouseService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryTransaction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    HandledByWorker = table.Column<string>(type: "text", nullable: false),
                    Comments = table.Column<string>(type: "text", nullable: false),
                    AttachmentIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    DraftItems = table.Column<JsonElement>(type: "jsonb", nullable: true),
                    Discriminator = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransaction", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemSchemas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AdditionalProperties = table.Column<List<string>>(type: "text[]", nullable: false),
                    AdditionalPropertiesTypes = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemSchemas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Item",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemSchemaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AdditionalProperties = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    Comments = table.Column<string>(type: "text", nullable: false),
                    InventoryTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Item", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Item_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Item_InventoryTransaction_InventoryTransactionId",
                        column: x => x.InventoryTransactionId,
                        principalTable: "InventoryTransaction",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Item_ItemSchemas_ItemSchemaId",
                        column: x => x.ItemSchemaId,
                        principalTable: "ItemSchemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Item_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ItemState",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Location = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RelatedTransactionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemState", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemState_InventoryTransaction_RelatedTransactionId",
                        column: x => x.RelatedTransactionId,
                        principalTable: "InventoryTransaction",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemState_Item_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemState_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Item_CustomerId",
                table: "Item",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Item_InventoryTransactionId",
                table: "Item",
                column: "InventoryTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Item_ItemSchemaId",
                table: "Item",
                column: "ItemSchemaId");

            migrationBuilder.CreateIndex(
                name: "IX_Item_WarehouseId",
                table: "Item",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemState_ItemId",
                table: "ItemState",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemState_RelatedTransactionId",
                table: "ItemState",
                column: "RelatedTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemState_WarehouseId",
                table: "ItemState",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemState");

            migrationBuilder.DropTable(
                name: "Item");

            migrationBuilder.DropTable(
                name: "InventoryTransaction");

            migrationBuilder.DropTable(
                name: "ItemSchemas");

            migrationBuilder.DropTable(
                name: "Warehouses");
        }
    }
}
