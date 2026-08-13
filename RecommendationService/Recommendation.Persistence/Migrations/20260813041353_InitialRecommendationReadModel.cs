using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recommendation.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialRecommendationReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Recommendation");

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                schema: "Recommendation",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMessages", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "PostSnapshots",
                schema: "Recommendation",
                columns: table => new
                {
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceVersion = table.Column<long>(type: "bigint", nullable: false),
                    BlogId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Visibility = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ProcessState = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    IsBanned = table.Column<bool>(type: "boolean", nullable: false),
                    PaymentSubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PreviewObjectName = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DurationSeconds = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ViewCount = table.Column<int>(type: "integer", nullable: false),
                    LikeCount = table.Column<int>(type: "integer", nullable: false),
                    DislikeCount = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostSnapshots", x => x.PostId);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationImpressions",
                schema: "Recommendation",
                columns: table => new
                {
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnonymousSessionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    AlgorithmVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CandidateSource = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    ShownAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationImpressions", x => new { x.RequestId, x.PostId });
                    table.CheckConstraint("CK_RecommendationImpressions_Subject", "(\"UserId\" IS NOT NULL AND \"AnonymousSessionId\" IS NULL) OR (\"UserId\" IS NULL AND \"AnonymousSessionId\" IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "UserBlogAffinities",
                schema: "Recommendation",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlogId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBlogAffinities", x => new { x.UserId, x.BlogId });
                });

            migrationBuilder.CreateTable(
                name: "UserCategoryAffinities",
                schema: "Recommendation",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCategoryAffinities", x => new { x.UserId, x.CategoryId });
                });

            migrationBuilder.CreateTable(
                name: "UserInteractions",
                schema: "Recommendation",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnonymousSessionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    InteractionType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    WatchedSeconds = table.Column<double>(type: "double precision", nullable: true),
                    WatchRatio = table.Column<double>(type: "double precision", nullable: true),
                    Reaction = table.Column<bool>(type: "boolean", nullable: true),
                    PreviousReaction = table.Column<bool>(type: "boolean", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInteractions", x => x.EventId);
                    table.CheckConstraint("CK_UserInteractions_Subject", "(\"UserId\" IS NOT NULL AND \"AnonymousSessionId\" IS NULL) OR (\"UserId\" IS NULL AND \"AnonymousSessionId\" IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "UserSubscriptions",
                schema: "Recommendation",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlogId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptions", x => new { x.UserId, x.BlogId });
                });

            migrationBuilder.CreateTable(
                name: "PostCategories",
                schema: "Recommendation",
                columns: table => new
                {
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostCategories", x => new { x.PostId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_PostCategories_PostSnapshots_PostId",
                        column: x => x.PostId,
                        principalSchema: "Recommendation",
                        principalTable: "PostSnapshots",
                        principalColumn: "PostId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_ProcessedAt_ReceivedAt",
                schema: "Recommendation",
                table: "InboxMessages",
                columns: new[] { "ProcessedAt", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PostCategories_CategoryId_PostId",
                schema: "Recommendation",
                table: "PostCategories",
                columns: new[] { "CategoryId", "PostId" });

            migrationBuilder.CreateIndex(
                name: "IX_PostSnapshots_BlogId_CreatedAt",
                schema: "Recommendation",
                table: "PostSnapshots",
                columns: new[] { "BlogId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PostSnapshots_UpdatedAt",
                schema: "Recommendation",
                table: "PostSnapshots",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PostSnapshots_Visibility_ProcessState_IsDeleted_IsBanned",
                schema: "Recommendation",
                table: "PostSnapshots",
                columns: new[] { "Visibility", "ProcessState", "IsDeleted", "IsBanned" });

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationImpressions_AnonymousSessionId_ShownAt",
                schema: "Recommendation",
                table: "RecommendationImpressions",
                columns: new[] { "AnonymousSessionId", "ShownAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationImpressions_PostId_ShownAt",
                schema: "Recommendation",
                table: "RecommendationImpressions",
                columns: new[] { "PostId", "ShownAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationImpressions_UserId_ShownAt",
                schema: "Recommendation",
                table: "RecommendationImpressions",
                columns: new[] { "UserId", "ShownAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserBlogAffinities_UserId_Score",
                schema: "Recommendation",
                table: "UserBlogAffinities",
                columns: new[] { "UserId", "Score" });

            migrationBuilder.CreateIndex(
                name: "IX_UserCategoryAffinities_UserId_Score",
                schema: "Recommendation",
                table: "UserCategoryAffinities",
                columns: new[] { "UserId", "Score" });

            migrationBuilder.CreateIndex(
                name: "IX_UserInteractions_AnonymousSessionId_OccurredAt",
                schema: "Recommendation",
                table: "UserInteractions",
                columns: new[] { "AnonymousSessionId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserInteractions_PostId_OccurredAt",
                schema: "Recommendation",
                table: "UserInteractions",
                columns: new[] { "PostId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserInteractions_UserId_OccurredAt",
                schema: "Recommendation",
                table: "UserInteractions",
                columns: new[] { "UserId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_BlogId_UserId",
                schema: "Recommendation",
                table: "UserSubscriptions",
                columns: new[] { "BlogId", "UserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "Recommendation");

            migrationBuilder.DropTable(
                name: "PostCategories",
                schema: "Recommendation");

            migrationBuilder.DropTable(
                name: "RecommendationImpressions",
                schema: "Recommendation");

            migrationBuilder.DropTable(
                name: "UserBlogAffinities",
                schema: "Recommendation");

            migrationBuilder.DropTable(
                name: "UserCategoryAffinities",
                schema: "Recommendation");

            migrationBuilder.DropTable(
                name: "UserInteractions",
                schema: "Recommendation");

            migrationBuilder.DropTable(
                name: "UserSubscriptions",
                schema: "Recommendation");

            migrationBuilder.DropTable(
                name: "PostSnapshots",
                schema: "Recommendation");
        }
    }
}
