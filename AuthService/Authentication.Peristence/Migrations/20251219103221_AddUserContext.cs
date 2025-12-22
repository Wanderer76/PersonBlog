using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authentication.Peristence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "Authentication",
                table: "AppUserRoles",
                keyColumns: new[] { "AppUserId", "UserRoleId" },
                keyValues: new object[] { new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"), new Guid("accbc12f-6ff1-4343-a26f-13b99e64abb6") });

            migrationBuilder.DropColumn(
                name: "BlogId",
                schema: "Authentication",
                table: "Profiles");

            migrationBuilder.CreateTable(
                name: "UserContexts",
                schema: "Authentication",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContextType = table.Column<int>(type: "integer", nullable: false),
                    ContextId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserContexts", x => new { x.UserId, x.ContextType, x.ContextId });
                    table.ForeignKey(
                        name: "FK_UserContexts_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalSchema: "Authentication",
                        principalTable: "AppUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                schema: "Authentication",
                table: "AppUserRoles",
                columns: new[] { "AppUserId", "UserRoleId" },
                values: new object[] { new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"), new Guid("d95ca3d6-0f63-4b48-a54f-1202f3d6bf2c") });

            migrationBuilder.UpdateData(
                schema: "Authentication",
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"),
                columns: new[] { "CreatedAt", "Password" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 12, 19, 10, 32, 20, 830, DateTimeKind.Unspecified).AddTicks(281), new TimeSpan(0, 0, 0, 0, 0)), "CZB/81yNwZ0=;iiFKYtT4M2RJ9teZbF6Ki/XVt3rSFv34NcyvoLCSoPA=" });

            migrationBuilder.UpdateData(
                schema: "Authentication",
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("c2ff298c-dd14-436c-a28b-e2036866ef41"),
                column: "Name",
                value: "blogger");

            migrationBuilder.InsertData(
                schema: "Authentication",
                table: "UserRoles",
                columns: new[] { "Id", "Name" },
                values: new object[] { new Guid("72c4bbed-5375-47fc-864f-1490ca82aced"), "artist" });

            migrationBuilder.CreateIndex(
                name: "IX_UserContexts_AppUserId",
                schema: "Authentication",
                table: "UserContexts",
                column: "AppUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserContexts",
                schema: "Authentication");

            migrationBuilder.DeleteData(
                schema: "Authentication",
                table: "AppUserRoles",
                keyColumns: new[] { "AppUserId", "UserRoleId" },
                keyValues: new object[] { new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"), new Guid("d95ca3d6-0f63-4b48-a54f-1202f3d6bf2c") });

            migrationBuilder.DeleteData(
                schema: "Authentication",
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("72c4bbed-5375-47fc-864f-1490ca82aced"));

            migrationBuilder.AddColumn<Guid>(
                name: "BlogId",
                schema: "Authentication",
                table: "Profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.InsertData(
                schema: "Authentication",
                table: "AppUserRoles",
                columns: new[] { "AppUserId", "UserRoleId" },
                values: new object[] { new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"), new Guid("accbc12f-6ff1-4343-a26f-13b99e64abb6") });

            migrationBuilder.UpdateData(
                schema: "Authentication",
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"),
                columns: new[] { "CreatedAt", "Password" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 8, 28, 6, 10, 3, 466, DateTimeKind.Unspecified).AddTicks(8242), new TimeSpan(0, 0, 0, 0, 0)), "39pgitWENkw=;vfwIBcvuNOIXDC4MgCw5ZkS89KILemL97F0SjbQ8tsU=" });

            migrationBuilder.UpdateData(
                schema: "Authentication",
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("c2ff298c-dd14-436c-a28b-e2036866ef41"),
                column: "Name",
                value: "bloger");
        }
    }
}
