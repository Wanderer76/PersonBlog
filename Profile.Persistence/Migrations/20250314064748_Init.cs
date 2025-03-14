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
                name: "PostViewers",
                schema: "Profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsLike = table.Column<bool>(type: "boolean", nullable: true),
                    IsViewed = table.Column<bool>(type: "boolean", nullable: false),
                    UserIpAddress = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostViewers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProfileEventMessages",
                schema: "Profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    EventData = table.Column<string>(type: "text", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileEventMessages", x => x.Id);
                });

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
                    PhotoUrl = table.Column<string>(type: "text", nullable: true),
                    ProfileState = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionLevels",
                schema: "Profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlogId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<double>(type: "double precision", nullable: false),
                    ImageId = table.Column<string>(type: "text", nullable: true),
                    NextLevelId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionLevels_SubscriptionLevels_NextLevelId",
                        column: x => x.NextLevelId,
                        principalSchema: "Profile",
                        principalTable: "SubscriptionLevels",
                        principalColumn: "Id");
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
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionsCount = table.Column<int>(type: "integer", nullable: false)
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
                name: "ProfileSubscriptions",
                schema: "Profile",
                columns: table => new
                {
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionLevelId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileSubscriptions", x => new { x.ProfileId, x.SubscriptionLevelId });
                    table.ForeignKey(
                        name: "FK_ProfileSubscriptions_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalSchema: "Profile",
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProfileSubscriptions_SubscriptionLevels_SubscriptionLevelId",
                        column: x => x.SubscriptionLevelId,
                        principalSchema: "Profile",
                        principalTable: "SubscriptionLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subscribers",
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
                    table.PrimaryKey("PK_Subscribers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscribers_Blogs_BlogId",
                        column: x => x.BlogId,
                        principalSchema: "Profile",
                        principalTable: "Blogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subscribers_Profiles_ProfileId",
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
                    PreviewId = table.Column<string>(type: "text", nullable: true),
                    VideoFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ViewCount = table.Column<int>(type: "integer", nullable: false),
                    LikeCount = table.Column<int>(type: "integer", nullable: false),
                    DislikeCount = table.Column<int>(type: "integer", nullable: false)
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
                    table.ForeignKey(
                        name: "FK_Posts_SubscriptionLevels_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalSchema: "Profile",
                        principalTable: "SubscriptionLevels",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VideoMetadata",
                schema: "Profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    Resolution = table.Column<int>(type: "integer", nullable: false),
                    Duration = table.Column<double>(type: "double precision", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ProcessState = table.Column<int>(type: "integer", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    FileExtension = table.Column<string>(type: "text", nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
                columns: new[] { "Id", "Birthdate", "CreatedAt", "Email", "FirstName", "IsDeleted", "LastName", "PhotoUrl", "ProfileState", "SurName", "UserId" },
                values: new object[] { new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"), null, new DateTimeOffset(new DateTime(2025, 3, 14, 6, 47, 47, 815, DateTimeKind.Unspecified).AddTicks(2160), new TimeSpan(0, 0, 0, 0, 0)), "ateplinsky@mail.ru", "Артём", false, null, null, 0, "Теплинский", new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e") });

            migrationBuilder.InsertData(
                schema: "Profile",
                table: "Blogs",
                columns: new[] { "Id", "CreatedAt", "Description", "PhotoUrl", "ProfileId", "SubscriptionsCount", "Title" },
                values: new object[] { new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"), new DateTimeOffset(new DateTime(2025, 3, 14, 6, 47, 47, 815, DateTimeKind.Unspecified).AddTicks(2774), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"), 0, "Тест" });

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
                name: "IX_Posts_SubscriptionId",
                schema: "Profile",
                table: "Posts",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_VideoFileId",
                schema: "Profile",
                table: "Posts",
                column: "VideoFileId");

            migrationBuilder.CreateIndex(
                name: "IX_PostViewers_UserId_PostId_UserIpAddress_CreatedAt",
                schema: "Profile",
                table: "PostViewers",
                columns: new[] { "UserId", "PostId", "UserIpAddress", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_UserId_IsDeleted",
                schema: "Profile",
                table: "Profiles",
                columns: new[] { "UserId", "IsDeleted" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfileSubscriptions_SubscriptionLevelId",
                schema: "Profile",
                table: "ProfileSubscriptions",
                column: "SubscriptionLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscribers_BlogId",
                schema: "Profile",
                table: "Subscribers",
                column: "BlogId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscribers_ProfileId_BlogId",
                schema: "Profile",
                table: "Subscribers",
                columns: new[] { "ProfileId", "BlogId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionLevels_NextLevelId",
                schema: "Profile",
                table: "SubscriptionLevels",
                column: "NextLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoMetadata_PostId_Resolution_ContentType",
                schema: "Profile",
                table: "VideoMetadata",
                columns: new[] { "PostId", "Resolution", "ContentType" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_VideoMetadata_VideoFileId",
                schema: "Profile",
                table: "Posts",
                column: "VideoFileId",
                principalSchema: "Profile",
                principalTable: "VideoMetadata",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Blogs_Profiles_ProfileId",
                schema: "Profile",
                table: "Blogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Blogs_BlogId",
                schema: "Profile",
                table: "Posts");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_SubscriptionLevels_SubscriptionId",
                schema: "Profile",
                table: "Posts");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_VideoMetadata_VideoFileId",
                schema: "Profile",
                table: "Posts");

            migrationBuilder.DropTable(
                name: "PostViewers",
                schema: "Profile");

            migrationBuilder.DropTable(
                name: "ProfileEventMessages",
                schema: "Profile");

            migrationBuilder.DropTable(
                name: "ProfileSubscriptions",
                schema: "Profile");

            migrationBuilder.DropTable(
                name: "Subscribers",
                schema: "Profile");

            migrationBuilder.DropTable(
                name: "Profiles",
                schema: "Profile");

            migrationBuilder.DropTable(
                name: "Blogs",
                schema: "Profile");

            migrationBuilder.DropTable(
                name: "SubscriptionLevels",
                schema: "Profile");

            migrationBuilder.DropTable(
                name: "VideoMetadata",
                schema: "Profile");

            migrationBuilder.DropTable(
                name: "Posts",
                schema: "Profile");
        }
    }
}
