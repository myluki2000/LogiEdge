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
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:hstore", ",,");

            migrationBuilder.CreateTable(
                name: "InboundTransactionPart",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboundTransactionPart", x => x.Id);
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
                name: "OutboundTransactionPart",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DraftPlaceholderItems = table.Column<List<Dictionary<string, string>>>(type: "hstore[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundTransactionPart", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RelocationTransactionPart",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelocationTransactionPart", x => x.Id);
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
                name: "ItemSchemaProperty",
                columns: table => new
                {
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Type = table.Column<int>(type: "integer", maxLength: 256, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsUnique = table.Column<bool>(type: "boolean", nullable: false),
                    ItemSchemaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemSchemaProperty", x => x.Name);
                    table.ForeignKey(
                        name: "FK_ItemSchemaProperty_ItemSchemas_ItemSchemaId",
                        column: x => x.ItemSchemaId,
                        principalTable: "ItemSchemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    HandledByWorker = table.Column<string>(type: "text", nullable: false),
                    AttachmentIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    InboundTransactionPartId = table.Column<Guid>(type: "uuid", nullable: true),
                    OutboundTransactionPartId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelocationTransactionPartId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_InboundTransactionPart_InboundTransac~",
                        column: x => x.InboundTransactionPartId,
                        principalTable: "InboundTransactionPart",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_OutboundTransactionPart_OutboundTrans~",
                        column: x => x.OutboundTransactionPartId,
                        principalTable: "OutboundTransactionPart",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_RelocationTransactionPart_RelocationT~",
                        column: x => x.RelocationTransactionPartId,
                        principalTable: "RelocationTransactionPart",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InboundDraftItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemSchemaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AdditionalProperties = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    Comments = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    IsQuarantined = table.Column<bool>(type: "boolean", nullable: false),
                    InboundTransactionPartId = table.Column<Guid>(type: "uuid", nullable: true)
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
                        name: "FK_InboundDraftItem_InboundTransactionPart_InboundTransactionP~",
                        column: x => x.InboundTransactionPartId,
                        principalTable: "InboundTransactionPart",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InboundDraftItem_ItemSchemas_ItemSchemaId",
                        column: x => x.ItemSchemaId,
                        principalTable: "ItemSchemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InboundDraftItem_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
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
                    OutboundTransactionPartId = table.Column<Guid>(type: "uuid", nullable: true),
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
                        name: "FK_Items_ItemSchemas_ItemSchemaId",
                        column: x => x.ItemSchemaId,
                        principalTable: "ItemSchemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Items_OutboundTransactionPart_OutboundTransactionPartId",
                        column: x => x.OutboundTransactionPartId,
                        principalTable: "OutboundTransactionPart",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Items_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Comment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Retracted = table.Column<bool>(type: "boolean", nullable: false),
                    InventoryTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comment_InventoryTransactions_InventoryTransactionId",
                        column: x => x.InventoryTransactionId,
                        principalTable: "InventoryTransactions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Comment_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
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
                    IsQuarantined = table.Column<bool>(type: "boolean", nullable: false),
                    RelatedTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    InboundTransactionPartId = table.Column<Guid>(type: "uuid", nullable: true),
                    OutboundTransactionPartId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelocationTransactionPartId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemState", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemState_InboundTransactionPart_InboundTransactionPartId",
                        column: x => x.InboundTransactionPartId,
                        principalTable: "InboundTransactionPart",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemState_InventoryTransactions_RelatedTransactionId",
                        column: x => x.RelatedTransactionId,
                        principalTable: "InventoryTransactions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemState_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemState_OutboundTransactionPart_OutboundTransactionPartId",
                        column: x => x.OutboundTransactionPartId,
                        principalTable: "OutboundTransactionPart",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemState_RelocationTransactionPart_RelocationTransactionPa~",
                        column: x => x.RelocationTransactionPartId,
                        principalTable: "RelocationTransactionPart",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemState_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comment_InventoryTransactionId",
                table: "Comment",
                column: "InventoryTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_ItemId",
                table: "Comment",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerItemSchema_ItemSchemaId",
                table: "CustomerItemSchema",
                column: "ItemSchemaId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundDraftItem_CustomerId",
                table: "InboundDraftItem",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundDraftItem_InboundTransactionPartId",
                table: "InboundDraftItem",
                column: "InboundTransactionPartId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundDraftItem_ItemSchemaId",
                table: "InboundDraftItem",
                column: "ItemSchemaId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundDraftItem_WarehouseId",
                table: "InboundDraftItem",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_InboundTransactionPartId",
                table: "InventoryTransactions",
                column: "InboundTransactionPartId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_OutboundTransactionPartId",
                table: "InventoryTransactions",
                column: "OutboundTransactionPartId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_RelocationTransactionPartId",
                table: "InventoryTransactions",
                column: "RelocationTransactionPartId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_CustomerId",
                table: "Items",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_ItemSchemaId",
                table: "Items",
                column: "ItemSchemaId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_OutboundTransactionPartId",
                table: "Items",
                column: "OutboundTransactionPartId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_WarehouseId",
                table: "Items",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemSchemaProperty_ItemSchemaId",
                table: "ItemSchemaProperty",
                column: "ItemSchemaId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemState_InboundTransactionPartId",
                table: "ItemState",
                column: "InboundTransactionPartId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemState_ItemId",
                table: "ItemState",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemState_OutboundTransactionPartId",
                table: "ItemState",
                column: "OutboundTransactionPartId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemState_RelatedTransactionId",
                table: "ItemState",
                column: "RelatedTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemState_RelocationTransactionPartId",
                table: "ItemState",
                column: "RelocationTransactionPartId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemState_WarehouseId",
                table: "ItemState",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Comment");

            migrationBuilder.DropTable(
                name: "CustomerItemSchema");

            migrationBuilder.DropTable(
                name: "InboundDraftItem");

            migrationBuilder.DropTable(
                name: "ItemSchemaProperty");

            migrationBuilder.DropTable(
                name: "ItemState");

            migrationBuilder.DropTable(
                name: "InventoryTransactions");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "InboundTransactionPart");

            migrationBuilder.DropTable(
                name: "RelocationTransactionPart");

            migrationBuilder.DropTable(
                name: "ItemSchemas");

            migrationBuilder.DropTable(
                name: "OutboundTransactionPart");

            migrationBuilder.DropTable(
                name: "Warehouses");
        }
    }
}
