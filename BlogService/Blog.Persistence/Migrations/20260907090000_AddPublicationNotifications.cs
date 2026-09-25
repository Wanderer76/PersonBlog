using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blog.Persistence.Migrations;

[DbContext(typeof(BlogDbContext))]
[Migration("20260907090000_AddPublicationNotifications")]
public partial class AddPublicationNotifications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "PublicationId",
            schema: "Blog",
            table: "Posts",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "PublishedAt",
            schema: "Blog",
            table: "Posts",
            type: "timestamp with time zone",
            nullable: true);

        // Existing public posts have already crossed the publication boundary.
        // Mark them without producing historical integration events.
        migrationBuilder.Sql(
            """
            UPDATE "Blog"."Posts"
            SET "PublicationId" = "Id", "PublishedAt" = "CreatedAt"
            WHERE "ProcessState" = 1
              AND "Visibility" = 0
              AND NOT "IsDelete"
              AND "BanMessageId" IS NULL;
            """);

        migrationBuilder.CreateIndex(
            name: "IX_Posts_PublicationId",
            schema: "Blog",
            table: "Posts",
            column: "PublicationId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Subscribers_BlogId_SubscriptionStartDate_UserId",
            schema: "Blog",
            table: "Subscribers",
            columns: new[] { "BlogId", "SubscriptionStartDate", "UserId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Posts_PublicationId",
            schema: "Blog",
            table: "Posts");

        migrationBuilder.DropIndex(
            name: "IX_Subscribers_BlogId_SubscriptionStartDate_UserId",
            schema: "Blog",
            table: "Subscribers");

        migrationBuilder.DropColumn(name: "PublicationId", schema: "Blog", table: "Posts");
        migrationBuilder.DropColumn(name: "PublishedAt", schema: "Blog", table: "Posts");
    }
}
