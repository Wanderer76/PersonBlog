using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoReacting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ADdDeleteDateTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeleteDateTime",
                schema: "VideoReacting",
                table: "UserPostViews",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeleteDateTime",
                schema: "VideoReacting",
                table: "UserPostViews");
        }
    }
}
