using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Music.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Artist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                schema: "Music",
                table: "Artists");

            migrationBuilder.AddColumn<short>(
                name: "Year",
                schema: "Music",
                table: "Tracks",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<Guid>(
                name: "AvatarId",
                schema: "Music",
                table: "Artists",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AvatarMetadata",
                schema: "Music",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    FileExtension = table.Column<string>(type: "text", nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ObjectName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvatarMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvatarMetadata_Artists_Id",
                        column: x => x.Id,
                        principalSchema: "Music",
                        principalTable: "Artists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvatarMetadata",
                schema: "Music");

            migrationBuilder.DropColumn(
                name: "Year",
                schema: "Music",
                table: "Tracks");

            migrationBuilder.DropColumn(
                name: "AvatarId",
                schema: "Music",
                table: "Artists");

            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                schema: "Music",
                table: "Artists",
                type: "text",
                nullable: true);
        }
    }
}
