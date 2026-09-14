using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoReacting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilePictureTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Birthdate",
                schema: "Profile",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "Email",
                schema: "Profile",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                schema: "Profile",
                table: "Profiles");

            migrationBuilder.CreateTable(
                name: "ProfilePictureFiles",
                schema: "Profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<long>(type: "bigint", nullable: false),
                    IsDelete = table.Column<bool>(type: "boolean", nullable: false),
                    DeleteDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    FileExtension = table.Column<string>(type: "text", nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ObjectName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfilePictureFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfilePictureFiles_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalSchema: "Profile",
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProfilePictureFiles_ProfileId",
                schema: "Profile",
                table: "ProfilePictureFiles",
                column: "ProfileId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfilePictureFiles",
                schema: "Profile");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Birthdate",
                schema: "Profile",
                table: "Profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "Profile",
                table: "Profiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                schema: "Profile",
                table: "Profiles",
                type: "text",
                nullable: true);
        }
    }
}
