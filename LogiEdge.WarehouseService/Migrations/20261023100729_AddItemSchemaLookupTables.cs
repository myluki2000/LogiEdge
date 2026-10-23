using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiEdge.WarehouseService.Migrations
{
    /// <inheritdoc />
    public partial class AddItemSchemaLookupTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemSchemaLookupColumns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemSchemaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KeySource = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemSchemaLookupColumns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemSchemaLookupColumns_ItemSchemas_ItemSchemaId",
                        column: x => x.ItemSchemaId,
                        principalTable: "ItemSchemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemSchemaLookupEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ColumnId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Value = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemSchemaLookupEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemSchemaLookupEntries_ItemSchemaLookupColumns_ColumnId",
                        column: x => x.ColumnId,
                        principalTable: "ItemSchemaLookupColumns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemSchemaLookupColumns_ItemSchemaId_Name",
                table: "ItemSchemaLookupColumns",
                columns: new[] { "ItemSchemaId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemSchemaLookupEntries_ColumnId_Key",
                table: "ItemSchemaLookupEntries",
                columns: new[] { "ColumnId", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemSchemaLookupEntries");

            migrationBuilder.DropTable(
                name: "ItemSchemaLookupColumns");
        }
    }
}
