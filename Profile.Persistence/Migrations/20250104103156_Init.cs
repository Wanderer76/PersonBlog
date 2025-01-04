using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Profile.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Profile");

            migrationBuilder.CreateTable(
                name: "Profiles",
                schema: "Profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    SurName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    Birthdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    PhotoUrl = table.Column<string>(type: "text", nullable: true),
                    ProfileState = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VideoUploadEvents",
                schema: "Profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileUrl = table.Column<string>(type: "text", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectName = table.Column<string>(type: "text", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoUploadEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Blogs",
                schema: "Profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PhotoUrl = table.Column<string>(type: "text", nullable: true),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Blogs_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalSchema: "Profile",
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Posts",
                schema: "Profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlogId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    VideoMetadataId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Posts_Blogs_BlogId",
                        column: x => x.BlogId,
                        principalSchema: "Profile",
                        principalTable: "Blogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                schema: "Profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlogId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionStartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SubscriptionEndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Blogs_BlogId",
                        column: x => x.BlogId,
                        principalSchema: "Profile",
                        principalTable: "Blogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalSchema: "Profile",
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoMetadata",
                schema: "Profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    FileExtension = table.Column<string>(type: "text", nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoMetadata_Posts_PostId",
                        column: x => x.PostId,
                        principalSchema: "Profile",
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "Profile",
                table: "Profiles",
                columns: new[] { "Id", "Birthdate", "Email", "FirstName", "IsDeleted", "LastName", "PhotoUrl", "ProfileState", "SurName", "UserId" },
                values: new object[] { new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d2e"), null, "ateplinsky@mail.ru", "Артём", false, null, null, 0, "Теплинский", new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e") });

            migrationBuilder.InsertData(
                schema: "Profile",
                table: "Blogs",
                columns: new[] { "Id", "CreatedAt", "Description", "PhotoUrl", "ProfileId", "Title" },
                values: new object[] { new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"), new DateTimeOffset(new DateTime(2025, 1, 4, 10, 31, 55, 898, DateTimeKind.Unspecified).AddTicks(9877), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d2e"), "Тест" });

            migrationBuilder.CreateIndex(
                name: "IX_Blogs_ProfileId",
                schema: "Profile",
                table: "Blogs",
                column: "ProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_BlogId",
                schema: "Profile",
                table: "Posts",
                column: "BlogId");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_UserId_IsDeleted",
                schema: "Profile",
                table: "Profiles",
                columns: new[] { "UserId", "IsDeleted" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_BlogId",
                schema: "Profile",
                table: "Subscriptions",
                column: "BlogId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_ProfileId_BlogId",
                schema: "Profile",
                table: "Subscriptions",
                columns: new[] { "ProfileId", "BlogId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoMetadata_PostId",
                schema: "Profile",
                table: "VideoMetadata",
                column: "PostId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Subscriptions",
                schema: "Profile");

            migrationBuilder.DropTable(
                name: "VideoMetadata",
                schema: "Profile");

            migrationBuilder.DropTable(
                name: "VideoUploadEvents",
                schema: "Profile");

            migrationBuilder.DropTable(
                name: "Posts",
                schema: "Profile");

            migrationBuilder.DropTable(
                name: "Blogs",
                schema: "Profile");

            migrationBuilder.DropTable(
                name: "Profiles",
                schema: "Profile");
        }
    }
}
