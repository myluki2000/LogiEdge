using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiEdge.Service.DocumentGenerator.Migrations
{
    /// <inheritdoc />
    public partial class AddInputFormHtml : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InputFormHtml",
                table: "DocumentTemplates",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InputFormHtml",
                table: "DocumentTemplates");
        }
    }
}
