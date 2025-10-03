using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Music.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tracks_ThumbnailId",
                schema: "Music",
                table: "Tracks");

            migrationBuilder.AlterColumn<long>(
                name: "Duration",
                schema: "Music",
                table: "TrackMetadata",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_ThumbnailId",
                schema: "Music",
                table: "Tracks",
                column: "ThumbnailId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tracks_ThumbnailId",
                schema: "Music",
                table: "Tracks");

            migrationBuilder.AlterColumn<double>(
                name: "Duration",
                schema: "Music",
                table: "TrackMetadata",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_ThumbnailId",
                schema: "Music",
                table: "Tracks",
                column: "ThumbnailId",
                unique: true);
        }
    }
}
