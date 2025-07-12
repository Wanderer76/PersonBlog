using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authentication.Peristence.Migrations
{
    /// <inheritdoc />
    public partial class AddBlogToProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BlogId",
                schema: "Authentication",
                table: "Profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "Authentication",
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"),
                columns: new[] { "CreatedAt", "Password" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 7, 12, 10, 7, 48, 380, DateTimeKind.Unspecified).AddTicks(7476), new TimeSpan(0, 0, 0, 0, 0)), "0ch1Fr5Rpq0=;lmdHoYpYQ4UtVmtKianfS/aeLUIlv5cJFrqHdwdcS7U=" });

            migrationBuilder.UpdateData(
                schema: "Authentication",
                table: "Profiles",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"),
                columns: new[] { "BlogId", "CreatedAt" },
                values: new object[] { null, new DateTimeOffset(new DateTime(2025, 7, 12, 10, 7, 48, 381, DateTimeKind.Unspecified).AddTicks(86), new TimeSpan(0, 0, 0, 0, 0)) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlogId",
                schema: "Authentication",
                table: "Profiles");

            migrationBuilder.UpdateData(
                schema: "Authentication",
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"),
                columns: new[] { "CreatedAt", "Password" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 4, 8, 6, 20, 19, 171, DateTimeKind.Unspecified).AddTicks(1102), new TimeSpan(0, 0, 0, 0, 0)), "Idh+v7iX7YM=;mCeHvmPNBSc7pqLyzYKc3uEgwqa37LVDdc1Mw0lnEpA=" });

            migrationBuilder.UpdateData(
                schema: "Authentication",
                table: "Profiles",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 4, 8, 6, 20, 19, 171, DateTimeKind.Unspecified).AddTicks(3562), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
