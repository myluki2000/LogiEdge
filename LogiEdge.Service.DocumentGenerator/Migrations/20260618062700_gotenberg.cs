using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiEdge.Service.DocumentGenerator.Migrations
{
    /// <inheritdoc />
    public partial class gotenberg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FooterCenter",
                table: "DocumentTemplates");

            migrationBuilder.DropColumn(
                name: "FooterLeft",
                table: "DocumentTemplates");

            migrationBuilder.DropColumn(
                name: "FooterRight",
                table: "DocumentTemplates");

            migrationBuilder.DropColumn(
                name: "HeaderCenter",
                table: "DocumentTemplates");

            migrationBuilder.RenameColumn(
                name: "HeaderRight",
                table: "DocumentTemplates",
                newName: "HeaderHtml");

            migrationBuilder.RenameColumn(
                name: "HeaderLeft",
                table: "DocumentTemplates",
                newName: "FooterHtml");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HeaderHtml",
                table: "DocumentTemplates",
                newName: "HeaderRight");

            migrationBuilder.RenameColumn(
                name: "FooterHtml",
                table: "DocumentTemplates",
                newName: "HeaderLeft");

            migrationBuilder.AddColumn<string>(
                name: "FooterCenter",
                table: "DocumentTemplates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FooterLeft",
                table: "DocumentTemplates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FooterRight",
                table: "DocumentTemplates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeaderCenter",
                table: "DocumentTemplates",
                type: "text",
                nullable: true);
        }
    }
}
