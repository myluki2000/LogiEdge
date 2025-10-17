using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LogiEdge.WarehouseService.Migrations
{
    /// <inheritdoc />
    public partial class ItemSchemaPropertyKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ItemSchemaProperty",
                table: "ItemSchemaProperty");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ItemSchemaProperty");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ItemSchemaProperty",
                table: "ItemSchemaProperty",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ItemSchemaProperty_ItemSchemaId",
                table: "ItemSchemaProperty",
                column: "ItemSchemaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ItemSchemaProperty",
                table: "ItemSchemaProperty");

            migrationBuilder.DropIndex(
                name: "IX_ItemSchemaProperty_ItemSchemaId",
                table: "ItemSchemaProperty");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "ItemSchemaProperty",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ItemSchemaProperty",
                table: "ItemSchemaProperty",
                columns: new[] { "ItemSchemaId", "Id" });
        }
    }
}
