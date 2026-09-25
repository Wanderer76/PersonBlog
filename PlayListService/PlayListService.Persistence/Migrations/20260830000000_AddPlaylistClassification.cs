using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlayListService.Domain.Entities;

#nullable disable

namespace PlayListService.Persistence.Migrations;

[DbContext(typeof(PlayListDbContext))]
[Migration("20260830000000_AddPlaylistClassification")]
public partial class AddPlaylistClassification : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ContentType",
            schema: "PlayList",
            table: "PlayLists",
            type: "integer",
            nullable: false,
            defaultValue: (int)PlayListContentType.Video);

        migrationBuilder.AddColumn<int>(
            name: "Kind",
            schema: "PlayList",
            table: "PlayLists",
            type: "integer",
            nullable: false,
            defaultValue: (int)PlayListKind.Authored);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ContentType",
            schema: "PlayList",
            table: "PlayLists");

        migrationBuilder.DropColumn(
            name: "Kind",
            schema: "PlayList",
            table: "PlayLists");
    }
}
