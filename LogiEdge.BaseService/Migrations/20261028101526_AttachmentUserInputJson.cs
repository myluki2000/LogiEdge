using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiEdge.BaseService.Migrations
{
    /// <inheritdoc />
    public partial class AttachmentUserInputJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserInputJson",
                table: "Attachments",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserInputJson",
                table: "Attachments");
        }
    }
}
