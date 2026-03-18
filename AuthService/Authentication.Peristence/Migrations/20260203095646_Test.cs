using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authentication.Peristence.Migrations
{
    /// <inheritdoc />
    public partial class Test : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserContexts_AppUsers_AppUserId",
                schema: "Authentication",
                table: "UserContexts");

            migrationBuilder.DropIndex(
                name: "IX_UserContexts_AppUserId",
                schema: "Authentication",
                table: "UserContexts");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                schema: "Authentication",
                table: "UserContexts");

            migrationBuilder.UpdateData(
                schema: "Authentication",
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"),
                columns: new[] { "CreatedAt", "Password" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 2, 3, 9, 56, 46, 276, DateTimeKind.Unspecified).AddTicks(5968), new TimeSpan(0, 0, 0, 0, 0)), "Qw9gP6bGfFQ=;iNNBDPwDvtSbWmEPhcEaPhdgcPxnf8t1907TMsM5suQ=" });

            migrationBuilder.AddForeignKey(
                name: "FK_UserContexts_AppUsers_UserId",
                schema: "Authentication",
                table: "UserContexts",
                column: "UserId",
                principalSchema: "Authentication",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserContexts_AppUsers_UserId",
                schema: "Authentication",
                table: "UserContexts");

            migrationBuilder.AddColumn<Guid>(
                name: "AppUserId",
                schema: "Authentication",
                table: "UserContexts",
                type: "uuid",
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "Authentication",
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"),
                columns: new[] { "CreatedAt", "Password" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 12, 19, 10, 32, 20, 830, DateTimeKind.Unspecified).AddTicks(281), new TimeSpan(0, 0, 0, 0, 0)), "CZB/81yNwZ0=;iiFKYtT4M2RJ9teZbF6Ki/XVt3rSFv34NcyvoLCSoPA=" });

            migrationBuilder.CreateIndex(
                name: "IX_UserContexts_AppUserId",
                schema: "Authentication",
                table: "UserContexts",
                column: "AppUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserContexts_AppUsers_AppUserId",
                schema: "Authentication",
                table: "UserContexts",
                column: "AppUserId",
                principalSchema: "Authentication",
                principalTable: "AppUsers",
                principalColumn: "Id");
        }
    }
}
