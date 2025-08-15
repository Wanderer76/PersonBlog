using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Authentication.Peristence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAppProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "Authentication",
                table: "Profiles",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"));

            migrationBuilder.DropColumn(
                name: "Birthdate",
                schema: "Authentication",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "FirstName",
                schema: "Authentication",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "LastName",
                schema: "Authentication",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "ProfileState",
                schema: "Authentication",
                table: "Profiles");

            migrationBuilder.RenameColumn(
                name: "SurName",
                schema: "Authentication",
                table: "Profiles",
                newName: "Name");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                schema: "Authentication",
                table: "Profiles",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.UpdateData(
                schema: "Authentication",
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"),
                columns: new[] { "CreatedAt", "Password" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 8, 14, 11, 19, 35, 473, DateTimeKind.Unspecified).AddTicks(9862), new TimeSpan(0, 0, 0, 0, 0)), "fQF9+RcYguA=;0NJCkwv4f80Bvev0SLLT0AEH7BhTNVTXEU8/DOXoGIQ=" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                schema: "Authentication",
                table: "Profiles",
                newName: "SurName");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                schema: "Authentication",
                table: "Profiles",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Birthdate",
                schema: "Authentication",
                table: "Profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                schema: "Authentication",
                table: "Profiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                schema: "Authentication",
                table: "Profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProfileState",
                schema: "Authentication",
                table: "Profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                schema: "Authentication",
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"),
                columns: new[] { "CreatedAt", "Password" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 7, 22, 13, 37, 56, 790, DateTimeKind.Unspecified).AddTicks(4584), new TimeSpan(0, 0, 0, 0, 0)), "fjD20nIO6+8=;NGMPtkI7jpL6Z4yAxr+YTYyJI6JzknYjRc1pwP5BnaI=" });

            migrationBuilder.InsertData(
                schema: "Authentication",
                table: "Profiles",
                columns: new[] { "Id", "Birthdate", "BlogId", "CreatedAt", "Email", "FirstName", "IsDeleted", "LastName", "PhotoUrl", "ProfileState", "SurName", "UserId" },
                values: new object[] { new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"), null, null, new DateTimeOffset(new DateTime(2025, 7, 22, 13, 37, 56, 790, DateTimeKind.Unspecified).AddTicks(7597), new TimeSpan(0, 0, 0, 0, 0)), "ateplinsky@mail.ru", "Артём", false, null, null, 0, "Теплинский", new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e") });
        }
    }
}
