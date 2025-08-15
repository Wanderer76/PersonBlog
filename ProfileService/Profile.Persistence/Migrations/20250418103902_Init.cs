using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoReacting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "VideoReacting");

            migrationBuilder.CreateTable(
                name: "UserPostViews",
                schema: "VideoReacting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    WatchedTime = table.Column<double>(type: "double precision", nullable: false),
                    IsCompleteWatch = table.Column<bool>(type: "boolean", nullable: false),
                    WatchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPostViews", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPostViews",
                schema: "VideoReacting");
        }
    }
}
