using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blog.Persistence.Migrations;

[DbContext(typeof(BlogDbContext))]
[Migration("20260819090000_MakePostViewerIpNullable")]
public partial class MakePostViewerIpNullable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "UserIpAddress",
            schema: "Blog",
            table: "PostViewers",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "UPDATE \"Blog\".\"PostViewers\" SET \"UserIpAddress\" = '' WHERE \"UserIpAddress\" IS NULL;");

        migrationBuilder.AlterColumn<string>(
            name: "UserIpAddress",
            schema: "Blog",
            table: "PostViewers",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);
    }
}
