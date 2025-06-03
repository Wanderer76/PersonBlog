using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoReacting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPostReaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PostReactions",
                schema: "VideoReacting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReactionTime = table.Column<double>(type: "double precision", nullable: false),
                    IsLike = table.Column<bool>(type: "boolean", nullable: true),
                    IsDelete = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostReactions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostReactions",
                schema: "VideoReacting");
        }
    }
}
