using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiEdge.Service.DocumentGenerator.Migrations
{
    /// <inheritdoc />
    public partial class DocumentTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    InputTypeName = table.Column<string>(type: "text", nullable: false),
                    Discriminator = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: false),
                    Html = table.Column<string>(type: "text", nullable: true),
                    HeaderLeft = table.Column<string>(type: "text", nullable: true),
                    HeaderCenter = table.Column<string>(type: "text", nullable: true),
                    HeaderRight = table.Column<string>(type: "text", nullable: true),
                    FooterLeft = table.Column<string>(type: "text", nullable: true),
                    FooterCenter = table.Column<string>(type: "text", nullable: true),
                    FooterRight = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTemplates", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentTemplates");
        }
    }
}
