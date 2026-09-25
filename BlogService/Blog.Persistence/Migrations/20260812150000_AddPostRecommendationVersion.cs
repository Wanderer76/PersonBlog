using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blog.Persistence.Migrations;

[DbContext(typeof(BlogDbContext))]
[Migration("20260812150000_AddPostRecommendationVersion")]
public partial class AddPostRecommendationVersion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "RecommendationVersion",
            schema: "Blog",
            table: "Posts",
            type: "bigint",
            nullable: false,
            defaultValue: 1L);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "RecommendationVersion",
            schema: "Blog",
            table: "Posts");
    }
}
