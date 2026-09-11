using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Conference.Persistence.Migrations;

[DbContext(typeof(ConferenceDbContext))]
[Migration("20260910120500_AddConferenceInvitationsAndOutbox")]
public sealed class AddConferenceInvitationsAndOutbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "OutboxMessages",
            schema: "Conference",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                State = table.Column<int>(type: "integer", nullable: false),
                EventType = table.Column<string>(type: "text", nullable: false),
                EventData = table.Column<string>(type: "text", nullable: false),
                RetryCount = table.Column<int>(type: "integer", nullable: false),
                ErrorMessage = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_OutboxMessages", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ConferenceInvitations",
            schema: "Conference",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ConferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                RecipientUserId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ConferenceInvitations", x => x.Id);
                table.ForeignKey("FK_ConferenceInvitations_ConferenceRooms_ConferenceId", x => x.ConferenceId,
                    principalSchema: "Conference", principalTable: "ConferenceRooms", principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ConferenceInvitations_ConferenceId_RecipientUserId",
            schema: "Conference", table: "ConferenceInvitations",
            columns: new[] { "ConferenceId", "RecipientUserId" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ConferenceInvitations", schema: "Conference");
        migrationBuilder.DropTable(name: "OutboxMessages", schema: "Conference");
    }
}
