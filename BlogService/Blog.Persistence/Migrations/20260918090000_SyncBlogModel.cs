using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blog.Persistence.Migrations;

[DbContext(typeof(BlogDbContext))]
[Migration("20260918090000_SyncBlogModel")]
public partial class SyncBlogModel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Subscribers_BlogId",
            schema: "Blog",
            table: "Subscribers");

        migrationBuilder.AlterColumn<string>(
            name: "Text",
            schema: "Blog",
            table: "TextPostInfos",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "UPDATE \"Blog\".\"TextPostInfos\" SET \"Text\" = '' WHERE \"Text\" IS NULL;");

        migrationBuilder.AlterColumn<string>(
            name: "Text",
            schema: "Blog",
            table: "TextPostInfos",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Subscribers_BlogId",
            schema: "Blog",
            table: "Subscribers",
            column: "BlogId");
    }
}
