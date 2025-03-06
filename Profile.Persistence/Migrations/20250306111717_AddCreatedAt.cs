using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Profile.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "Profile",
                table: "Profiles",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d2e"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "Profile",
                table: "Profiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                schema: "Profile",
                table: "Blogs",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 3, 6, 11, 17, 16, 675, DateTimeKind.Unspecified).AddTicks(3116), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.InsertData(
                schema: "Profile",
                table: "Profiles",
                columns: new[] { "Id", "Birthdate", "CreatedAt", "Email", "FirstName", "IsDeleted", "LastName", "PhotoUrl", "ProfileState", "SurName", "UserId" },
                values: new object[] { new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"), null, new DateTimeOffset(new DateTime(2025, 3, 6, 11, 17, 16, 675, DateTimeKind.Unspecified).AddTicks(2761), new TimeSpan(0, 0, 0, 0, 0)), "ateplinsky@mail.ru", "Артём", false, null, null, 0, "Теплинский", new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e") });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "Profile",
                table: "Profiles",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"));

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "Profile",
                table: "Profiles");

            migrationBuilder.UpdateData(
                schema: "Profile",
                table: "Blogs",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 2, 25, 5, 40, 33, 856, DateTimeKind.Unspecified).AddTicks(8473), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.InsertData(
                schema: "Profile",
                table: "Profiles",
                columns: new[] { "Id", "Birthdate", "Email", "FirstName", "IsDeleted", "LastName", "PhotoUrl", "ProfileState", "SurName", "UserId" },
                values: new object[] { new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d2e"), null, "ateplinsky@mail.ru", "Артём", false, null, null, 0, "Теплинский", new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e") });
        }
    }
}
