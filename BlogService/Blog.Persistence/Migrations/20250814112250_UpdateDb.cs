using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Profile.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Blog");

            migrationBuilder.RenameTable(
                name: "VideoProcessingSagaStates",
                schema: "Profile",
                newName: "VideoProcessingSagaStates",
                newSchema: "Blog");

            migrationBuilder.RenameTable(
                name: "VideoMetadata",
                schema: "Profile",
                newName: "VideoMetadata",
                newSchema: "Blog");

            migrationBuilder.RenameTable(
                name: "Subscribers",
                schema: "Profile",
                newName: "Subscribers",
                newSchema: "Blog");

            migrationBuilder.RenameTable(
                name: "ProfileSubscriptions",
                schema: "Profile",
                newName: "ProfileSubscriptions",
                newSchema: "Blog");

            migrationBuilder.RenameTable(
                name: "ProfileEventMessages",
                schema: "Profile",
                newName: "ProfileEventMessages",
                newSchema: "Blog");

            migrationBuilder.RenameTable(
                name: "PostViewers",
                schema: "Profile",
                newName: "PostViewers",
                newSchema: "Blog");

            migrationBuilder.RenameTable(
                name: "Posts",
                schema: "Profile",
                newName: "Posts",
                newSchema: "Blog");

            migrationBuilder.RenameTable(
                name: "PostRemoveEvents",
                schema: "Profile",
                newName: "PostRemoveEvents",
                newSchema: "Blog");

            migrationBuilder.RenameTable(
                name: "PlayLists",
                schema: "Profile",
                newName: "PlayLists",
                newSchema: "Blog");

            migrationBuilder.RenameTable(
                name: "PlayListItems",
                schema: "Profile",
                newName: "PlayListItems",
                newSchema: "Blog");

            migrationBuilder.RenameTable(
                name: "PaymentSubscriptions",
                schema: "Profile",
                newName: "PaymentSubscriptions",
                newSchema: "Blog");

            migrationBuilder.RenameTable(
                name: "Blogs",
                schema: "Profile",
                newName: "Blogs",
                newSchema: "Blog");

            migrationBuilder.CreateTable(
                name: "Categories",
                schema: "Blog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PostCategory",
                schema: "Blog",
                columns: table => new
                {
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostCategory", x => new { x.PostId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_PostCategory_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "Blog",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostCategory_Posts_PostId",
                        column: x => x.PostId,
                        principalSchema: "Blog",
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                schema: "Blog",
                table: "Blogs",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 8, 14, 11, 22, 49, 990, DateTimeKind.Unspecified).AddTicks(685), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.InsertData(
                schema: "Blog",
                table: "Categories",
                columns: new[] { "Id", "Title" },
                values: new object[,]
                {
                    { 1, "Развлечения" },
                    { 2, "Музыка" },
                    { 3, "Игры" },
                    { 4, "Образование" },
                    { 5, "Наука и технологии" },
                    { 6, "Спорт" },
                    { 7, "Кино и анимация" },
                    { 8, "Новости и политика" },
                    { 9, "Авто и транспорт" },
                    { 10, "Путешествия и события" },
                    { 11, "Образ жизни" },
                    { 12, "Кулинария" },
                    { 13, "Юмор" },
                    { 14, "Личный блог" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostCategory_CategoryId",
                schema: "Blog",
                table: "PostCategory",
                column: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostCategory",
                schema: "Blog");

            migrationBuilder.DropTable(
                name: "Categories",
                schema: "Blog");

            migrationBuilder.EnsureSchema(
                name: "Profile");

            migrationBuilder.RenameTable(
                name: "VideoProcessingSagaStates",
                schema: "Blog",
                newName: "VideoProcessingSagaStates",
                newSchema: "Profile");

            migrationBuilder.RenameTable(
                name: "VideoMetadata",
                schema: "Blog",
                newName: "VideoMetadata",
                newSchema: "Profile");

            migrationBuilder.RenameTable(
                name: "Subscribers",
                schema: "Blog",
                newName: "Subscribers",
                newSchema: "Profile");

            migrationBuilder.RenameTable(
                name: "ProfileSubscriptions",
                schema: "Blog",
                newName: "ProfileSubscriptions",
                newSchema: "Profile");

            migrationBuilder.RenameTable(
                name: "ProfileEventMessages",
                schema: "Blog",
                newName: "ProfileEventMessages",
                newSchema: "Profile");

            migrationBuilder.RenameTable(
                name: "PostViewers",
                schema: "Blog",
                newName: "PostViewers",
                newSchema: "Profile");

            migrationBuilder.RenameTable(
                name: "Posts",
                schema: "Blog",
                newName: "Posts",
                newSchema: "Profile");

            migrationBuilder.RenameTable(
                name: "PostRemoveEvents",
                schema: "Blog",
                newName: "PostRemoveEvents",
                newSchema: "Profile");

            migrationBuilder.RenameTable(
                name: "PlayLists",
                schema: "Blog",
                newName: "PlayLists",
                newSchema: "Profile");

            migrationBuilder.RenameTable(
                name: "PlayListItems",
                schema: "Blog",
                newName: "PlayListItems",
                newSchema: "Profile");

            migrationBuilder.RenameTable(
                name: "PaymentSubscriptions",
                schema: "Blog",
                newName: "PaymentSubscriptions",
                newSchema: "Profile");

            migrationBuilder.RenameTable(
                name: "Blogs",
                schema: "Blog",
                newName: "Blogs",
                newSchema: "Profile");

            migrationBuilder.UpdateData(
                schema: "Profile",
                table: "Blogs",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 6, 9, 12, 10, 18, 950, DateTimeKind.Unspecified).AddTicks(4094), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
