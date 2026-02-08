using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zaczy.SongBook.Migrations
{
    /// <inheritdoc />
    public partial class SongDuration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "scrolling_tempo",
                table: "songs",
                newName: "song_duration");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "song_duration",
                table: "songs",
                newName: "scrolling_tempo");
        }
    }
}
