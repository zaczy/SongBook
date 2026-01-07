using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zaczy.SongBook.Migrations
{
    /// <inheritdoc />
    public partial class AUthorsColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "source_html",
                table: "songs",
                newName: "comments");

            migrationBuilder.AddColumn<string>(
                name: "lyrics_author",
                table: "songs",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "music_author",
                table: "songs",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "lyrics_author",
                table: "songs");

            migrationBuilder.DropColumn(
                name: "music_author",
                table: "songs");

            migrationBuilder.RenameColumn(
                name: "comments",
                table: "songs",
                newName: "source_html");
        }
    }
}
