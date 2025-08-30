using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
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
                name: "CustomerItemSchema",
                columns: table => new
                {
                    CustomersId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemSchemaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerItemSchema", x => new { x.CustomersId, x.ItemSchemaId });
                    table.ForeignKey(
                        name: "FK_CustomerItemSchema_Customers_CustomersId",
                        column: x => x.CustomersId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerItemSchema_ItemSchemas_ItemSchemaId",
                        column: x => x.ItemSchemaId,
                        principalTable: "ItemSchemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InboundDraftItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemSchemaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemNumber = table.Column<string>(type: "text", nullable: false),
                    AdditionalProperties = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    Comments = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    InboundTransactionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboundDraftItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InboundDraftItem_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InboundDraftItem_InventoryTransaction_InboundTransactionId",
                        column: x => x.InboundTransactionId,
                        principalTable: "InventoryTransaction",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InboundDraftItem_ItemSchemas_ItemSchemaId",
                        column: x => x.ItemSchemaId,
                        principalTable: "ItemSchemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemSchemaProperty",
                columns: table => new
                {
                    ItemSchemaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Type = table.Column<int>(type: "integer", maxLength: 256, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsUnique = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemSchemaProperty", x => new { x.ItemSchemaId, x.Id });
                    table.ForeignKey(
                        name: "FK_ItemSchemaProperty_ItemSchemas_ItemSchemaId",
                        column: x => x.ItemSchemaId,
                        principalTable: "ItemSchemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Items",
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
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Items_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Items_InventoryTransaction_InventoryTransactionId",
                        column: x => x.InventoryTransactionId,
                        principalTable: "InventoryTransaction",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Items_ItemSchemas_ItemSchemaId",
                        column: x => x.ItemSchemaId,
                        principalTable: "ItemSchemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Items_Warehouses_WarehouseId",
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
                        name: "FK_ItemState_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
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
                name: "IX_CustomerItemSchema_ItemSchemaId",
                table: "CustomerItemSchema",
                column: "ItemSchemaId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundDraftItem_CustomerId",
                table: "InboundDraftItem",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundDraftItem_InboundTransactionId",
                table: "InboundDraftItem",
                column: "InboundTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundDraftItem_ItemSchemaId",
                table: "InboundDraftItem",
                column: "ItemSchemaId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_CustomerId",
                table: "Items",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_InventoryTransactionId",
                table: "Items",
                column: "InventoryTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_ItemSchemaId",
                table: "Items",
                column: "ItemSchemaId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_WarehouseId",
                table: "Items",
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
                name: "CustomerItemSchema");

            migrationBuilder.DropTable(
                name: "InboundDraftItem");

            migrationBuilder.DropTable(
                name: "ItemSchemaProperty");

            migrationBuilder.DropTable(
                name: "ItemState");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "InventoryTransaction");

            migrationBuilder.DropTable(
                name: "ItemSchemas");

            migrationBuilder.DropTable(
                name: "Warehouses");
        }
    }
}
