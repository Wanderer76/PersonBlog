using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authentication.Peristence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                schema: "Authentication",
                table: "Profiles");

            migrationBuilder.UpdateData(
                schema: "Authentication",
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"),
                columns: new[] { "CreatedAt", "Password" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 2, 27, 5, 58, 21, 279, DateTimeKind.Unspecified).AddTicks(871), new TimeSpan(0, 0, 0, 0, 0)), "O0VWS+HeCMg=;R6kqABwX+gHnnmjHT+tNt3Rpb3AbqUItP5EqBkKML8s=" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "Authentication",
                table: "Profiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                schema: "Authentication",
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"),
                columns: new[] { "CreatedAt", "Password" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 2, 3, 9, 56, 46, 276, DateTimeKind.Unspecified).AddTicks(5968), new TimeSpan(0, 0, 0, 0, 0)), "Qw9gP6bGfFQ=;iNNBDPwDvtSbWmEPhcEaPhdgcPxnf8t1907TMsM5suQ=" });
        }
    }
}
