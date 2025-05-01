using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Profile.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Blogs_Profiles_ProfileId",
                schema: "Profile",
                table: "Blogs");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfileSubscriptions_Profiles_ProfileId",
                schema: "Profile",
                table: "ProfileSubscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscribers_Profiles_ProfileId",
                schema: "Profile",
                table: "Subscribers");

            migrationBuilder.DropTable(
                name: "Profiles",
                schema: "Profile");

            migrationBuilder.RenameColumn(
                name: "ProfileId",
                schema: "Profile",
                table: "Subscribers",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Subscribers_ProfileId_BlogId",
                schema: "Profile",
                table: "Subscribers",
                newName: "IX_Subscribers_UserId_BlogId");

            migrationBuilder.RenameColumn(
                name: "ProfileId",
                schema: "Profile",
                table: "ProfileSubscriptions",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "ProfileId",
                schema: "Profile",
                table: "Blogs",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Blogs_ProfileId",
                schema: "Profile",
                table: "Blogs",
                newName: "IX_Blogs_UserId");

            migrationBuilder.UpdateData(
                schema: "Profile",
                table: "Blogs",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 4, 8, 6, 21, 7, 552, DateTimeKind.Unspecified).AddTicks(5772), new TimeSpan(0, 0, 0, 0, 0)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "Profile",
                table: "Subscribers",
                newName: "ProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_Subscribers_UserId_BlogId",
                schema: "Profile",
                table: "Subscribers",
                newName: "IX_Subscribers_ProfileId_BlogId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "Profile",
                table: "ProfileSubscriptions",
                newName: "ProfileId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "Profile",
                table: "Blogs",
                newName: "ProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_Blogs_UserId",
                schema: "Profile",
                table: "Blogs",
                newName: "IX_Blogs_ProfileId");

            migrationBuilder.CreateTable(
                name: "Profiles",
                schema: "Profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Birthdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    PhotoUrl = table.Column<string>(type: "text", nullable: true),
                    ProfileState = table.Column<int>(type: "integer", nullable: false),
                    SurName = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.Id);
                });

            migrationBuilder.UpdateData(
                schema: "Profile",
                table: "Blogs",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 3, 14, 6, 47, 47, 815, DateTimeKind.Unspecified).AddTicks(2774), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.InsertData(
                schema: "Profile",
                table: "Profiles",
                columns: new[] { "Id", "Birthdate", "CreatedAt", "Email", "FirstName", "IsDeleted", "LastName", "PhotoUrl", "ProfileState", "SurName", "UserId" },
                values: new object[] { new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"), null, new DateTimeOffset(new DateTime(2025, 3, 14, 6, 47, 47, 815, DateTimeKind.Unspecified).AddTicks(2160), new TimeSpan(0, 0, 0, 0, 0)), "ateplinsky@mail.ru", "Артём", false, null, null, 0, "Теплинский", new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e") });

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_UserId_IsDeleted",
                schema: "Profile",
                table: "Profiles",
                columns: new[] { "UserId", "IsDeleted" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Blogs_Profiles_ProfileId",
                schema: "Profile",
                table: "Blogs",
                column: "ProfileId",
                principalSchema: "Profile",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfileSubscriptions_Profiles_ProfileId",
                schema: "Profile",
                table: "ProfileSubscriptions",
                column: "ProfileId",
                principalSchema: "Profile",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscribers_Profiles_ProfileId",
                schema: "Profile",
                table: "Subscribers",
                column: "ProfileId",
                principalSchema: "Profile",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
