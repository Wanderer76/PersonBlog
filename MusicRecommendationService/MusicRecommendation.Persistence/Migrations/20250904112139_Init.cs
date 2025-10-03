using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicRecommendation.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "MusicRecommendation");

            migrationBuilder.CreateTable(
                name: "Tracks",
                schema: "MusicRecommendation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlbumId = table.Column<Guid>(type: "uuid", nullable: true),
                    ArtistId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tracks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserListenHistory",
                schema: "MusicRecommendation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TrackId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserListenHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserTrackAuditions",
                schema: "MusicRecommendation",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrackId = table.Column<Guid>(type: "uuid", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    UpdateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTrackAuditions", x => new { x.UserId, x.TrackId });
                });

            migrationBuilder.CreateTable(
                name: "TrackGenres",
                schema: "MusicRecommendation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrackId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackGenres", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackGenres_Tracks_TrackId",
                        column: x => x.TrackId,
                        principalSchema: "MusicRecommendation",
                        principalTable: "Tracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrackGenres_TrackId",
                schema: "MusicRecommendation",
                table: "TrackGenres",
                column: "TrackId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrackGenres",
                schema: "MusicRecommendation");

            migrationBuilder.DropTable(
                name: "UserListenHistory",
                schema: "MusicRecommendation");

            migrationBuilder.DropTable(
                name: "UserTrackAuditions",
                schema: "MusicRecommendation");

            migrationBuilder.DropTable(
                name: "Tracks",
                schema: "MusicRecommendation");
        }
    }
}
