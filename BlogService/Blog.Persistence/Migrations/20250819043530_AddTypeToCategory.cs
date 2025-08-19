using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Profile.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTypeToCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                schema: "Blog",
                table: "Categories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                schema: "Blog",
                table: "Blogs",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                schema: "Blog",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "Type",
                value: 1);

            migrationBuilder.UpdateData(
                schema: "Blog",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "Type",
                value: 2);

            migrationBuilder.UpdateData(
                schema: "Blog",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "Type",
                value: 3);

            migrationBuilder.UpdateData(
                schema: "Blog",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "Type",
                value: 4);

            migrationBuilder.UpdateData(
                schema: "Blog",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "Type",
                value: 5);

            migrationBuilder.UpdateData(
                schema: "Blog",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6,
                column: "Type",
                value: 6);

            migrationBuilder.UpdateData(
                schema: "Blog",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 7,
                column: "Type",
                value: 7);

            migrationBuilder.UpdateData(
                schema: "Blog",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 8,
                column: "Type",
                value: 8);

            migrationBuilder.UpdateData(
                schema: "Blog",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 9,
                column: "Type",
                value: 9);

            migrationBuilder.UpdateData(
                schema: "Blog",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 10,
                column: "Type",
                value: 10);

            migrationBuilder.UpdateData(
                schema: "Blog",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 11,
                column: "Type",
                value: 11);

            migrationBuilder.UpdateData(
                schema: "Blog",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 12,
                column: "Type",
                value: 12);

            migrationBuilder.UpdateData(
                schema: "Blog",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 13,
                column: "Type",
                value: 13);

            migrationBuilder.UpdateData(
                schema: "Blog",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 14,
                column: "Type",
                value: 14);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                schema: "Blog",
                table: "Categories");

            migrationBuilder.UpdateData(
                schema: "Blog",
                table: "Blogs",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 8, 18, 9, 59, 22, 754, DateTimeKind.Unspecified).AddTicks(6925), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
