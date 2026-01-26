using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zaczy.SongBook.Migrations
{
    /// <inheritdoc />
    public partial class ScrollingColums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "scrolling_delay",
                table: "songs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "scrolling_tempo",
                table: "songs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "spotify_link",
                table: "songs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "scrolling_delay",
                table: "songs");

            migrationBuilder.DropColumn(
                name: "scrolling_tempo",
                table: "songs");

            migrationBuilder.DropColumn(
                name: "spotify_link",
                table: "songs");
        }
    }
}
