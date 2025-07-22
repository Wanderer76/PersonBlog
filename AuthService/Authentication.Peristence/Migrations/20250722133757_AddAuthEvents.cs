using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authentication.Peristence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthEvents",
                schema: "Authentication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    EventData = table.Column<string>(type: "text", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthEvents", x => x.Id);
                });

            migrationBuilder.UpdateData(
                schema: "Authentication",
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"),
                columns: new[] { "CreatedAt", "Password" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 7, 22, 13, 37, 56, 790, DateTimeKind.Unspecified).AddTicks(4584), new TimeSpan(0, 0, 0, 0, 0)), "fjD20nIO6+8=;NGMPtkI7jpL6Z4yAxr+YTYyJI6JzknYjRc1pwP5BnaI=" });

            migrationBuilder.UpdateData(
                schema: "Authentication",
                table: "Profiles",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 7, 22, 13, 37, 56, 790, DateTimeKind.Unspecified).AddTicks(7597), new TimeSpan(0, 0, 0, 0, 0)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthEvents",
                schema: "Authentication");

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
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 7, 12, 10, 7, 48, 381, DateTimeKind.Unspecified).AddTicks(86), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
