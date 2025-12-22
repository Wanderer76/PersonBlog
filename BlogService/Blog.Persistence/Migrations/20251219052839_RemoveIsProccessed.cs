using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Profile.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsProccessed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "Blog",
                table: "Blogs",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"));

            migrationBuilder.DropColumn(
                name: "IsProcessed",
                schema: "Blog",
                table: "VideoMetadata");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsProcessed",
                schema: "Blog",
                table: "VideoMetadata",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.InsertData(
                schema: "Blog",
                table: "Blogs",
                columns: new[] { "Id", "CreatedAt", "Description", "PhotoUrl", "SubscriptionsCount", "Title", "UserId" },
                values: new object[] { new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"), new DateTimeOffset(new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, 0, "Тест", new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e") });
        }
    }
}
