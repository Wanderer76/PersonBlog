using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Search.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Search");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:public.btree_gin", ",,")
                .Annotation("Npgsql:PostgresExtension:public.pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "PostIndices",
                schema: "Search",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlogId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ViewCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostIndices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WordScores",
                schema: "Search",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Word = table.Column<string>(type: "text", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    PostIndexId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WordScores_PostIndices_PostIndexId",
                        column: x => x.PostIndexId,
                        principalSchema: "Search",
                        principalTable: "PostIndices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_WordScores_PostIndexId",
                schema: "Search",
                table: "WordScores",
                column: "PostIndexId");

            migrationBuilder.Sql(@"
CREATE INDEX idx_posts_title_trgm ON ""Search"".""PostIndices""  USING GIN (""Title"" public.gin_trgm_ops);

CREATE INDEX idx_post_keywords_word_trgm ON ""Search"".""WordScores""  USING GIN (""Word"" public.gin_trgm_ops);
CREATE INDEX idx_posts_created_at ON ""Search"".""PostIndices""  (""CreatedAt"" DESC);
CREATE INDEX idx_posts_view_count ON ""Search"".""PostIndices"" (""ViewCount"" DESC);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WordScores",
                schema: "Search");

            migrationBuilder.DropTable(
                name: "PostIndices",
                schema: "Search");
        }
    }
}
