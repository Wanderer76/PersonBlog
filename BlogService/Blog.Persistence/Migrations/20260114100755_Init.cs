using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Blog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Blog");

            migrationBuilder.CreateTable(
                name: "Blogs",
                schema: "Blog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PhotoUrl = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionsCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                schema: "Blog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentSubscriptions",
                schema: "Blog",
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
                    table.PrimaryKey("PK_PaymentSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentSubscriptions_PaymentSubscriptions_NextLevelId",
                        column: x => x.NextLevelId,
                        principalSchema: "Blog",
                        principalTable: "PaymentSubscriptions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PostRemoveEvents",
                schema: "Blog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostRemoveEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PostViewers",
                schema: "Blog",
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
                schema: "Blog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
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
                name: "VideoMetadata",
                schema: "Blog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Resolution = table.Column<int>(type: "integer", nullable: false),
                    Duration = table.Column<double>(type: "double precision", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
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
                });

            migrationBuilder.CreateTable(
                name: "VideoProcessingSagaStates",
                schema: "Blog",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentState = table.Column<string>(type: "text", nullable: false),
                    VideoMetadataId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoProcessingSagaStates", x => x.CorrelationId);
                });

            migrationBuilder.CreateTable(
                name: "Posts",
                schema: "Blog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlogId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    PaymentSubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ViewCount = table.Column<int>(type: "integer", nullable: false),
                    LikeCount = table.Column<int>(type: "integer", nullable: false),
                    DislikeCount = table.Column<int>(type: "integer", nullable: false),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    ProcessState = table.Column<int>(type: "integer", nullable: false),
                    IsDelete = table.Column<bool>(type: "boolean", nullable: false),
                    DeleteDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    BanMessageId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Posts_Blogs_BlogId",
                        column: x => x.BlogId,
                        principalSchema: "Blog",
                        principalTable: "Blogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subscribers",
                schema: "Blog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
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
                        principalSchema: "Blog",
                        principalTable: "Blogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfileSubscriptions",
                schema: "Blog",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionLevelId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileSubscriptions", x => new { x.UserId, x.SubscriptionLevelId });
                    table.ForeignKey(
                        name: "FK_ProfileSubscriptions_PaymentSubscriptions_SubscriptionLevel~",
                        column: x => x.SubscriptionLevelId,
                        principalSchema: "Blog",
                        principalTable: "PaymentSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BanMessages",
                schema: "Blog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    BannedDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    BanReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BanMessages_Posts_PostId",
                        column: x => x.PostId,
                        principalSchema: "Blog",
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TextPostInfos",
                schema: "Blog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TextPostInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TextPostInfos_Posts_Id",
                        column: x => x.Id,
                        principalSchema: "Blog",
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostFiles",
                schema: "Blog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    TextPostInfoId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    FileExtension = table.Column<string>(type: "text", nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ObjectName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostFiles_TextPostInfos_TextPostInfoId",
                        column: x => x.TextPostInfoId,
                        principalSchema: "Blog",
                        principalTable: "TextPostInfos",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VideoPostInfos",
                schema: "Blog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    PreviewId = table.Column<Guid>(type: "uuid", nullable: true),
                    VideoFileId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoPostInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoPostInfos_PostFiles_PreviewId",
                        column: x => x.PreviewId,
                        principalSchema: "Blog",
                        principalTable: "PostFiles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VideoPostInfos_Posts_Id",
                        column: x => x.Id,
                        principalSchema: "Blog",
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VideoPostInfos_VideoMetadata_VideoFileId",
                        column: x => x.VideoFileId,
                        principalSchema: "Blog",
                        principalTable: "VideoMetadata",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PostCategory",
                schema: "Blog",
                columns: table => new
                {
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    VideoPostInfoId = table.Column<Guid>(type: "uuid", nullable: true)
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
                    table.ForeignKey(
                        name: "FK_PostCategory_VideoPostInfos_VideoPostInfoId",
                        column: x => x.VideoPostInfoId,
                        principalSchema: "Blog",
                        principalTable: "VideoPostInfos",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                schema: "Blog",
                table: "Categories",
                columns: new[] { "Id", "Title", "Type" },
                values: new object[,]
                {
                    { 1, "Развлечения", 1 },
                    { 2, "Музыка", 2 },
                    { 3, "Игры", 3 },
                    { 4, "Образование", 4 },
                    { 5, "Наука и технологии", 5 },
                    { 6, "Спорт", 6 },
                    { 7, "Кино и анимация", 7 },
                    { 8, "Новости и политика", 8 },
                    { 9, "Авто и транспорт", 9 },
                    { 10, "Путешествия и события", 10 },
                    { 11, "Образ жизни", 11 },
                    { 12, "Кулинария", 12 },
                    { 13, "Юмор", 13 },
                    { 14, "Личный блог", 14 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BanMessages_PostId",
                schema: "Blog",
                table: "BanMessages",
                column: "PostId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Blogs_UserId",
                schema: "Blog",
                table: "Blogs",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSubscriptions_NextLevelId",
                schema: "Blog",
                table: "PaymentSubscriptions",
                column: "NextLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_PostCategory_CategoryId",
                schema: "Blog",
                table: "PostCategory",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PostCategory_VideoPostInfoId",
                schema: "Blog",
                table: "PostCategory",
                column: "VideoPostInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_PostFiles_TextPostInfoId",
                schema: "Blog",
                table: "PostFiles",
                column: "TextPostInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_BlogId",
                schema: "Blog",
                table: "Posts",
                column: "BlogId");

            migrationBuilder.CreateIndex(
                name: "IX_PostViewers_UserId_PostId_UserIpAddress_CreatedAt",
                schema: "Blog",
                table: "PostViewers",
                columns: new[] { "UserId", "PostId", "UserIpAddress", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfileSubscriptions_SubscriptionLevelId",
                schema: "Blog",
                table: "ProfileSubscriptions",
                column: "SubscriptionLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscribers_BlogId",
                schema: "Blog",
                table: "Subscribers",
                column: "BlogId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscribers_UserId_BlogId",
                schema: "Blog",
                table: "Subscribers",
                columns: new[] { "UserId", "BlogId" });

            migrationBuilder.CreateIndex(
                name: "IX_VideoMetadata_PostId_Resolution_ContentType",
                schema: "Blog",
                table: "VideoMetadata",
                columns: new[] { "PostId", "Resolution", "ContentType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoPostInfos_PreviewId",
                schema: "Blog",
                table: "VideoPostInfos",
                column: "PreviewId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoPostInfos_VideoFileId",
                schema: "Blog",
                table: "VideoPostInfos",
                column: "VideoFileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BanMessages",
                schema: "Blog");

            migrationBuilder.DropTable(
                name: "PostCategory",
                schema: "Blog");

            migrationBuilder.DropTable(
                name: "PostRemoveEvents",
                schema: "Blog");

            migrationBuilder.DropTable(
                name: "PostViewers",
                schema: "Blog");

            migrationBuilder.DropTable(
                name: "ProfileEventMessages",
                schema: "Blog");

            migrationBuilder.DropTable(
                name: "ProfileSubscriptions",
                schema: "Blog");

            migrationBuilder.DropTable(
                name: "Subscribers",
                schema: "Blog");

            migrationBuilder.DropTable(
                name: "VideoProcessingSagaStates",
                schema: "Blog");

            migrationBuilder.DropTable(
                name: "Categories",
                schema: "Blog");

            migrationBuilder.DropTable(
                name: "VideoPostInfos",
                schema: "Blog");

            migrationBuilder.DropTable(
                name: "PaymentSubscriptions",
                schema: "Blog");

            migrationBuilder.DropTable(
                name: "PostFiles",
                schema: "Blog");

            migrationBuilder.DropTable(
                name: "VideoMetadata",
                schema: "Blog");

            migrationBuilder.DropTable(
                name: "TextPostInfos",
                schema: "Blog");

            migrationBuilder.DropTable(
                name: "Posts",
                schema: "Blog");

            migrationBuilder.DropTable(
                name: "Blogs",
                schema: "Blog");
        }
    }
}
