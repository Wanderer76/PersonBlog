using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicRecommendation.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Init2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TrackGenres",
                schema: "MusicRecommendation",
                table: "TrackGenres");

            migrationBuilder.DropIndex(
                name: "IX_TrackGenres_TrackId",
                schema: "MusicRecommendation",
                table: "TrackGenres");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrackGenres",
                schema: "MusicRecommendation",
                table: "TrackGenres",
                columns: new[] { "TrackId", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TrackGenres",
                schema: "MusicRecommendation",
                table: "TrackGenres");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrackGenres",
                schema: "MusicRecommendation",
                table: "TrackGenres",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_TrackGenres_TrackId",
                schema: "MusicRecommendation",
                table: "TrackGenres",
                column: "TrackId");
        }
    }
}
