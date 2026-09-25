using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blog.Persistence.Migrations;

[DbContext(typeof(BlogDbContext))]
[Migration("20260914090000_RenameLegacyProfileArtifacts")]
public partial class RenameLegacyProfileArtifacts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameTable(
            name: "ProfileEventMessages",
            schema: "Blog",
            newName: "OutboxMessages",
            newSchema: "Blog");

        migrationBuilder.RenameTable(
            name: "ProfileSubscriptions",
            schema: "Blog",
            newName: "PaymentSubscribers",
            newSchema: "Blog");

        migrationBuilder.RenameIndex(
            name: "IX_ProfileSubscriptions_SubscriptionLevelId",
            schema: "Blog",
            table: "PaymentSubscribers",
            newName: "IX_PaymentSubscribers_SubscriptionLevelId");

        migrationBuilder.Sql(
            "ALTER TABLE \"Blog\".\"OutboxMessages\" RENAME CONSTRAINT \"PK_ProfileEventMessages\" TO \"PK_OutboxMessages\";");
        migrationBuilder.Sql(
            "ALTER TABLE \"Blog\".\"PaymentSubscribers\" RENAME CONSTRAINT \"PK_ProfileSubscriptions\" TO \"PK_PaymentSubscribers\";");
        migrationBuilder.Sql(
            "ALTER TABLE \"Blog\".\"PaymentSubscribers\" RENAME CONSTRAINT \"FK_ProfileSubscriptions_PaymentSubscriptions_SubscriptionLevel~\" TO \"FK_PaymentSubscribers_PaymentSubscriptions_SubscriptionLevelId\";");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "ALTER TABLE \"Blog\".\"OutboxMessages\" RENAME CONSTRAINT \"PK_OutboxMessages\" TO \"PK_ProfileEventMessages\";");
        migrationBuilder.Sql(
            "ALTER TABLE \"Blog\".\"PaymentSubscribers\" RENAME CONSTRAINT \"PK_PaymentSubscribers\" TO \"PK_ProfileSubscriptions\";");
        migrationBuilder.Sql(
            "ALTER TABLE \"Blog\".\"PaymentSubscribers\" RENAME CONSTRAINT \"FK_PaymentSubscribers_PaymentSubscriptions_SubscriptionLevelId\" TO \"FK_ProfileSubscriptions_PaymentSubscriptions_SubscriptionLevel~\";");

        migrationBuilder.RenameIndex(
            name: "IX_PaymentSubscribers_SubscriptionLevelId",
            schema: "Blog",
            table: "PaymentSubscribers",
            newName: "IX_ProfileSubscriptions_SubscriptionLevelId");

        migrationBuilder.RenameTable(
            name: "OutboxMessages",
            schema: "Blog",
            newName: "ProfileEventMessages",
            newSchema: "Blog");

        migrationBuilder.RenameTable(
            name: "PaymentSubscribers",
            schema: "Blog",
            newName: "ProfileSubscriptions",
            newSchema: "Blog");
    }
}
