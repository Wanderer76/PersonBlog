using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Profile.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddResolution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Resolution",
                schema: "Profile",
                table: "VideoMetadata",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                schema: "Profile",
                table: "Blogs",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 1, 9, 8, 26, 14, 612, DateTimeKind.Unspecified).AddTicks(3426), new TimeSpan(0, 0, 0, 0, 0)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Resolution",
                schema: "Profile",
                table: "VideoMetadata");

            migrationBuilder.UpdateData(
                schema: "Profile",
                table: "Blogs",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 1, 4, 10, 31, 55, 898, DateTimeKind.Unspecified).AddTicks(9877), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
