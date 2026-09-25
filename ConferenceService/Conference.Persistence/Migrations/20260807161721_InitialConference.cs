using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Conference.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialConference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Conference");

            migrationBuilder.CreateTable(
                name: "ConferenceRooms",
                schema: "Conference",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConferenceRooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConferenceParticipants",
                schema: "Conference",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConferenceRoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConferenceParticipants", x => new { x.ConferenceRoomId, x.SessionId });
                    table.ForeignKey(
                        name: "FK_ConferenceParticipants_ConferenceRooms_ConferenceRoomId",
                        column: x => x.ConferenceRoomId,
                        principalSchema: "Conference",
                        principalTable: "ConferenceRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                schema: "Conference",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_ConferenceRooms_ConferenceId",
                        column: x => x.ConferenceId,
                        principalSchema: "Conference",
                        principalTable: "ConferenceRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConferenceParticipants_ConferenceRoomId_UserId",
                schema: "Conference",
                table: "ConferenceParticipants",
                columns: new[] { "ConferenceRoomId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConferenceId",
                schema: "Conference",
                table: "Messages",
                column: "ConferenceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConferenceParticipants",
                schema: "Conference");

            migrationBuilder.DropTable(
                name: "Messages",
                schema: "Conference");

            migrationBuilder.DropTable(
                name: "ConferenceRooms",
                schema: "Conference");
        }
    }
}
