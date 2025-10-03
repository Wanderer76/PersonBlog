using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Music.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayLists",
                schema: "Music",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayListTracks",
                schema: "Music",
                columns: table => new
                {
                    PlayListId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrackId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayListTracks", x => new { x.TrackId, x.PlayListId });
                    table.ForeignKey(
                        name: "FK_PlayListTracks_PlayLists_PlayListId",
                        column: x => x.PlayListId,
                        principalSchema: "Music",
                        principalTable: "PlayLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayListTracks_Tracks_TrackId",
                        column: x => x.TrackId,
                        principalSchema: "Music",
                        principalTable: "Tracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayListTracks_PlayListId",
                schema: "Music",
                table: "PlayListTracks",
                column: "PlayListId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayListTracks",
                schema: "Music");

            migrationBuilder.DropTable(
                name: "PlayLists",
                schema: "Music");
        }
    }
}
