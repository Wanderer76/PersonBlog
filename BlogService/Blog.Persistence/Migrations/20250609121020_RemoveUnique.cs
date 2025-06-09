using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Profile.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscribers_UserId_BlogId",
                schema: "Profile",
                table: "Subscribers");

            migrationBuilder.UpdateData(
                schema: "Profile",
                table: "Blogs",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 6, 9, 12, 10, 18, 950, DateTimeKind.Unspecified).AddTicks(4094), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_Subscribers_UserId_BlogId",
                schema: "Profile",
                table: "Subscribers",
                columns: new[] { "UserId", "BlogId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscribers_UserId_BlogId",
                schema: "Profile",
                table: "Subscribers");

            migrationBuilder.UpdateData(
                schema: "Profile",
                table: "Blogs",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 6, 9, 10, 50, 39, 157, DateTimeKind.Unspecified).AddTicks(1237), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_Subscribers_UserId_BlogId",
                schema: "Profile",
                table: "Subscribers",
                columns: new[] { "UserId", "BlogId" },
                unique: true);
        }
    }
}
