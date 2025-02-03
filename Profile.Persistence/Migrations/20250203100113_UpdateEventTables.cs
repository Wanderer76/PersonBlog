using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Profile.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEventTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "Profile",
                table: "VideoUploadEvents");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                schema: "Profile",
                table: "VideoUploadEvents");

            migrationBuilder.DropColumn(
                name: "State",
                schema: "Profile",
                table: "VideoUploadEvents");

            migrationBuilder.UpdateData(
                schema: "Profile",
                table: "Blogs",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 2, 3, 10, 1, 11, 714, DateTimeKind.Unspecified).AddTicks(7654), new TimeSpan(0, 0, 0, 0, 0)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "Profile",
                table: "VideoUploadEvents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                schema: "Profile",
                table: "VideoUploadEvents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "State",
                schema: "Profile",
                table: "VideoUploadEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                schema: "Profile",
                table: "Blogs",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 1, 31, 7, 57, 3, 129, DateTimeKind.Unspecified).AddTicks(6652), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
