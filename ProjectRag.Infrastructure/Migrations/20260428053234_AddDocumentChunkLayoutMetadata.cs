using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectRag.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentChunkLayoutMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BoundingRegionsJson",
                table: "DocumentChunks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LayoutRole",
                table: "DocumentChunks",
                type: "TEXT",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BoundingRegionsJson",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "LayoutRole",
                table: "DocumentChunks");
        }
    }
}
