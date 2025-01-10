using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Profile.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VideoMetadata_PostId",
                schema: "Profile",
                table: "VideoMetadata");

            migrationBuilder.UpdateData(
                schema: "Profile",
                table: "Blogs",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 1, 10, 6, 24, 13, 505, DateTimeKind.Unspecified).AddTicks(8038), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_VideoMetadata_PostId_Resolution_ContentType",
                schema: "Profile",
                table: "VideoMetadata",
                columns: new[] { "PostId", "Resolution", "ContentType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VideoMetadata_PostId_Resolution_ContentType",
                schema: "Profile",
                table: "VideoMetadata");

            migrationBuilder.UpdateData(
                schema: "Profile",
                table: "Blogs",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 1, 10, 4, 50, 7, 556, DateTimeKind.Unspecified).AddTicks(151), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_VideoMetadata_PostId",
                schema: "Profile",
                table: "VideoMetadata",
                column: "PostId");
        }
    }
}
