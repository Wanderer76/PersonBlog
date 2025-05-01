using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Authentication.Peristence.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Authentication");

            migrationBuilder.CreateTable(
                name: "AppUsers",
                schema: "Authentication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Login = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastAuthenticate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                schema: "Authentication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tokens",
                schema: "Authentication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AppUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Login = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tokens_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalSchema: "Authentication",
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppUserRoles",
                schema: "Authentication",
                columns: table => new
                {
                    AppUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserRoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUserRoles", x => x.AppUserId);
                    table.ForeignKey(
                        name: "FK_AppUserRoles_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalSchema: "Authentication",
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppUserRoles_UserRoles_UserRoleId",
                        column: x => x.UserRoleId,
                        principalSchema: "Authentication",
                        principalTable: "UserRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "Authentication",
                table: "AppUsers",
                columns: new[] { "Id", "CreatedAt", "LastAuthenticate", "Login", "Password" },
                values: new object[] { new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"), new DateTimeOffset(new DateTime(2025, 1, 3, 11, 45, 58, 704, DateTimeKind.Unspecified).AddTicks(6585), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "admin", "bk/QhiCzne0=;zyfj5njzkC02n26/1GjRdDR/3j2IoEofTbE5qONczTI=" });

            migrationBuilder.InsertData(
                schema: "Authentication",
                table: "UserRoles",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("57a2b99b-b6ee-4c98-a1f0-b18fe96dae60"), "admin" },
                    { new Guid("accbc12f-6ff1-4343-a26f-13b99e64abb6"), "superadmin" },
                    { new Guid("c2ff298c-dd14-436c-a28b-e2036866ef41"), "bloger" },
                    { new Guid("d95ca3d6-0f63-4b48-a54f-1202f3d6bf2c"), "user" }
                });

            migrationBuilder.InsertData(
                schema: "Authentication",
                table: "AppUserRoles",
                columns: new[] { "AppUserId", "UserRoleId" },
                values: new object[] { new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"), new Guid("accbc12f-6ff1-4343-a26f-13b99e64abb6") });

            migrationBuilder.CreateIndex(
                name: "IX_AppUserRoles_UserRoleId",
                schema: "Authentication",
                table: "AppUserRoles",
                column: "UserRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_AppUserId",
                schema: "Authentication",
                table: "Tokens",
                column: "AppUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppUserRoles",
                schema: "Authentication");

            migrationBuilder.DropTable(
                name: "Tokens",
                schema: "Authentication");

            migrationBuilder.DropTable(
                name: "UserRoles",
                schema: "Authentication");

            migrationBuilder.DropTable(
                name: "AppUsers",
                schema: "Authentication");
        }
    }
}
