using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Profile.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayLists",
                schema: "Profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlogId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ThumbnailId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayListItems",
                schema: "Profile",
                columns: table => new
                {
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayListId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayListItems", x => new { x.PlayListId, x.PostId });
                    table.ForeignKey(
                        name: "FK_PlayListItems_PlayLists_PlayListId",
                        column: x => x.PlayListId,
                        principalSchema: "Profile",
                        principalTable: "PlayLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                schema: "Profile",
                table: "Blogs",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 4, 28, 6, 38, 59, 176, DateTimeKind.Unspecified).AddTicks(2442), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_PlayListItems_PlayListId_PostId_Position",
                schema: "Profile",
                table: "PlayListItems",
                columns: new[] { "PlayListId", "PostId", "Position" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayListItems",
                schema: "Profile");

            migrationBuilder.DropTable(
                name: "PlayLists",
                schema: "Profile");

            migrationBuilder.UpdateData(
                schema: "Profile",
                table: "Blogs",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 4, 18, 10, 43, 7, 120, DateTimeKind.Unspecified).AddTicks(3843), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
