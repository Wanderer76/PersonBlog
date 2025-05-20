using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Profile.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCorelationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CorrelationId",
                schema: "Profile",
                table: "ProfileEventMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "Profile",
                table: "Blogs",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 5, 18, 8, 36, 39, 295, DateTimeKind.Unspecified).AddTicks(506), new TimeSpan(0, 0, 0, 0, 0)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorrelationId",
                schema: "Profile",
                table: "ProfileEventMessages");

            migrationBuilder.UpdateData(
                schema: "Profile",
                table: "Blogs",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 5, 5, 11, 41, 53, 871, DateTimeKind.Unspecified).AddTicks(3381), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
