using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Microblog.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPostEmbeddingData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "EmbeddingData",
                table: "Posts",
                type: "varbinary(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmbeddingData",
                table: "Posts");
        }
    }
}
