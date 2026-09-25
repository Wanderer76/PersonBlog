using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlayListService.Persistence.Migrations;

[DbContext(typeof(PlayListDbContext))]
[Migration("20260828000000_AlignThumbnailModel")]
public partial class AlignThumbnailModel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "PlayList"."PlayLists"
            ALTER COLUMN "ThumbnailId" TYPE uuid
            USING NULLIF("ThumbnailId", '')::uuid;
            """);

        migrationBuilder.CreateTable(
            name: "PlayListFile",
            schema: "PlayList",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PlaylistId = table.Column<Guid>(type: "uuid", nullable: true),
                Name = table.Column<string>(type: "text", nullable: false),
                FileExtension = table.Column<string>(type: "text", nullable: false),
                Length = table.Column<long>(type: "bigint", nullable: false),
                ContentType = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ObjectName = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PlayListFile", x => x.Id);
                table.ForeignKey(
                    name: "FK_PlayListFile_PlayLists_PlaylistId",
                    column: x => x.PlaylistId,
                    principalSchema: "PlayList",
                    principalTable: "PlayLists",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_PlayListFile_PlaylistId",
            schema: "PlayList",
            table: "PlayListFile",
            column: "PlaylistId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "PlayListFile",
            schema: "PlayList");

        migrationBuilder.Sql(
            """
            ALTER TABLE "PlayList"."PlayLists"
            ALTER COLUMN "ThumbnailId" TYPE text
            USING "ThumbnailId"::text;
            """);
    }
}
