using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VideoReacting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAppProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Profile");

            migrationBuilder.RenameTable(
                name: "UserPostViews",
                schema: "VideoReacting",
                newName: "UserPostViews",
                newSchema: "Profile");

            migrationBuilder.RenameTable(
                name: "SubscribedChanels",
                schema: "VideoReacting",
                newName: "SubscribedChanels",
                newSchema: "Profile");

            migrationBuilder.RenameTable(
                name: "ReactingEvents",
                schema: "VideoReacting",
                newName: "ReactingEvents",
                newSchema: "Profile");

            migrationBuilder.RenameTable(
                name: "PostReactions",
                schema: "VideoReacting",
                newName: "PostReactions",
                newSchema: "Profile");

            migrationBuilder.CreateTable(
                name: "Profiles",
                schema: "Profile",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Birthdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhotoUrl = table.Column<string>(type: "text", nullable: true),
                    ProfileState = table.Column<int>(type: "integer", nullable: false),
                    BlogId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_UserId",
                schema: "Profile",
                table: "Profiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Profiles",
                schema: "Profile");

            migrationBuilder.EnsureSchema(
                name: "VideoReacting");

            migrationBuilder.RenameTable(
                name: "UserPostViews",
                schema: "Profile",
                newName: "UserPostViews",
                newSchema: "VideoReacting");

            migrationBuilder.RenameTable(
                name: "SubscribedChanels",
                schema: "Profile",
                newName: "SubscribedChanels",
                newSchema: "VideoReacting");

            migrationBuilder.RenameTable(
                name: "ReactingEvents",
                schema: "Profile",
                newName: "ReactingEvents",
                newSchema: "VideoReacting");

            migrationBuilder.RenameTable(
                name: "PostReactions",
                schema: "Profile",
                newName: "PostReactions",
                newSchema: "VideoReacting");
        }
    }
}
