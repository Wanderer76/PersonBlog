using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Profile.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVideoModelAndEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCompleted",
                schema: "Profile",
                table: "VideoUploadEvents");

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

            migrationBuilder.AddColumn<double>(
                name: "Duration",
                schema: "Profile",
                table: "VideoMetadata",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.UpdateData(
                schema: "Profile",
                table: "Blogs",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 1, 31, 7, 57, 3, 129, DateTimeKind.Unspecified).AddTicks(6652), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_VideoMetadata_PostId",
                schema: "Profile",
                table: "VideoMetadata",
                column: "PostId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VideoMetadata_PostId",
                schema: "Profile",
                table: "VideoMetadata");

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

            migrationBuilder.DropColumn(
                name: "Duration",
                schema: "Profile",
                table: "VideoMetadata");

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                schema: "Profile",
                table: "VideoUploadEvents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                schema: "Profile",
                table: "Blogs",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 1, 26, 14, 21, 55, 151, DateTimeKind.Unspecified).AddTicks(2192), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
