using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authentication.Peristence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Profiles",
                schema: "Authentication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    SurName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    Birthdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhotoUrl = table.Column<string>(type: "text", nullable: true),
                    ProfileState = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.Id);
                });

            migrationBuilder.UpdateData(
                schema: "Authentication",
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"),
                columns: new[] { "CreatedAt", "Password" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 4, 8, 6, 20, 19, 171, DateTimeKind.Unspecified).AddTicks(1102), new TimeSpan(0, 0, 0, 0, 0)), "Idh+v7iX7YM=;mCeHvmPNBSc7pqLyzYKc3uEgwqa37LVDdc1Mw0lnEpA=" });

            migrationBuilder.InsertData(
                schema: "Authentication",
                table: "Profiles",
                columns: new[] { "Id", "Birthdate", "CreatedAt", "Email", "FirstName", "IsDeleted", "LastName", "PhotoUrl", "ProfileState", "SurName", "UserId" },
                values: new object[] { new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"), null, new DateTimeOffset(new DateTime(2025, 4, 8, 6, 20, 19, 171, DateTimeKind.Unspecified).AddTicks(3562), new TimeSpan(0, 0, 0, 0, 0)), "ateplinsky@mail.ru", "Артём", false, null, null, 0, "Теплинский", new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e") });

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_UserId_IsDeleted",
                schema: "Authentication",
                table: "Profiles",
                columns: new[] { "UserId", "IsDeleted" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Profiles",
                schema: "Authentication");

            migrationBuilder.UpdateData(
                schema: "Authentication",
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"),
                columns: new[] { "CreatedAt", "Password" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 1, 3, 11, 45, 58, 704, DateTimeKind.Unspecified).AddTicks(6585), new TimeSpan(0, 0, 0, 0, 0)), "bk/QhiCzne0=;zyfj5njzkC02n26/1GjRdDR/3j2IoEofTbE5qONczTI=" });
        }
    }
}
