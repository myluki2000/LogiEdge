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
                name: "InventoryTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EventDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BookedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    HandledByWorker = table.Column<string>(type: "text", nullable: false),
                    AttachmentIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactions", x => x.Id);
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
                name: "InboundTransactionParts",
                columns: table => new
                {
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboundTransactionParts", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_InboundTransactionParts_InventoryTransactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "InventoryTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutboundTransactionParts",
                columns: table => new
                {
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DraftPlaceholderItems = table.Column<List<Dictionary<string, string>>>(type: "hstore[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundTransactionParts", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_OutboundTransactionParts_InventoryTransactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "InventoryTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RelocationTransactionParts",
                columns: table => new
                {
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelocationTransactionParts", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_RelocationTransactionParts_InventoryTransactions_Transactio~",
                        column: x => x.TransactionId,
                        principalTable: "InventoryTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransactionEditLogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartType = table.Column<int>(type: "integer", nullable: true),
                    EditedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EditedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeSummary = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ChangesJson = table.Column<JsonDocument>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionEditLogEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionEditLogEntries_InventoryTransactions_Transaction~",
                        column: x => x.TransactionId,
                        principalTable: "InventoryTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "InboundDraftItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemSchemaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AdditionalProperties = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    Comments = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    IsQuarantined = table.Column<bool>(type: "boolean", nullable: false),
                    InboundTransactionPartTransactionId = table.Column<Guid>(type: "uuid", nullable: true)
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
                        name: "FK_InboundDraftItem_InboundTransactionParts_InboundTransaction~",
                        column: x => x.InboundTransactionPartTransactionId,
                        principalTable: "InboundTransactionParts",
                        principalColumn: "TransactionId");
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
                    OutboundTransactionPartTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
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
                        name: "FK_Items_OutboundTransactionParts_OutboundTransactionPartTrans~",
                        column: x => x.OutboundTransactionPartTransactionId,
                        principalTable: "OutboundTransactionParts",
                        principalColumn: "TransactionId");
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
                name: "ItemsStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Location = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsQuarantined = table.Column<bool>(type: "boolean", nullable: false),
                    RelatedTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    InventoryTransactionPartTransactionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemsStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemsStates_InventoryTransactions_RelatedTransactionId",
                        column: x => x.RelatedTransactionId,
                        principalTable: "InventoryTransactions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemsStates_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemsStates_Warehouses_WarehouseId",
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
                name: "IX_InboundDraftItem_InboundTransactionPartTransactionId",
                table: "InboundDraftItem",
                column: "InboundTransactionPartTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundDraftItem_ItemSchemaId",
                table: "InboundDraftItem",
                column: "ItemSchemaId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundDraftItem_WarehouseId",
                table: "InboundDraftItem",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_BookedDate",
                table: "InventoryTransactions",
                column: "BookedDate",
                unique: true,
                filter: "\"BookedDate\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Items_CustomerId",
                table: "Items",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_ItemSchemaId",
                table: "Items",
                column: "ItemSchemaId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_OutboundTransactionPartTransactionId",
                table: "Items",
                column: "OutboundTransactionPartTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_WarehouseId",
                table: "Items",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemSchemaProperty_ItemSchemaId",
                table: "ItemSchemaProperty",
                column: "ItemSchemaId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemsStates_InventoryTransactionPartTransactionId",
                table: "ItemsStates",
                column: "InventoryTransactionPartTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemsStates_ItemId",
                table: "ItemsStates",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemsStates_RelatedTransactionId",
                table: "ItemsStates",
                column: "RelatedTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemsStates_WarehouseId",
                table: "ItemsStates",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionEditLogEntries_TransactionId",
                table: "TransactionEditLogEntries",
                column: "TransactionId");
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
                name: "ItemsStates");

            migrationBuilder.DropTable(
                name: "RelocationTransactionParts");

            migrationBuilder.DropTable(
                name: "TransactionEditLogEntries");

            migrationBuilder.DropTable(
                name: "InboundTransactionParts");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "ItemSchemas");

            migrationBuilder.DropTable(
                name: "OutboundTransactionParts");

            migrationBuilder.DropTable(
                name: "Warehouses");

            migrationBuilder.DropTable(
                name: "InventoryTransactions");
        }
    }
}
