using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notification.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialNotificationStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Notification");

            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                schema: "Notification",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => new { x.UserId, x.Kind, x.Channel });
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                schema: "Notification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Producer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationWork",
                schema: "Notification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    DedupKey = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: true),
                    Cursor = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LeaseToken = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaseUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AvailableAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Channel = table.Column<int>(type: "integer", nullable: true),
                    DestinationKey = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationWork", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationWork_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalSchema: "Notification",
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Unread",
                schema: "Notification",
                table: "Notifications",
                columns: new[] { "UserId", "CreatedAt", "Id" },
                filter: "\"ReadAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_CreatedAt_Id",
                schema: "Notification",
                table: "Notifications",
                columns: new[] { "UserId", "CreatedAt", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_Kind_BusinessId",
                schema: "Notification",
                table: "Notifications",
                columns: new[] { "UserId", "Kind", "BusinessId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationWork_Kind_DedupKey",
                schema: "Notification",
                table: "NotificationWork",
                columns: new[] { "Kind", "DedupKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationWork_Kind_Status_AvailableAt_LeaseUntil",
                schema: "Notification",
                table: "NotificationWork",
                columns: new[] { "Kind", "Status", "AvailableAt", "LeaseUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationWork_NotificationId",
                schema: "Notification",
                table: "NotificationWork",
                column: "NotificationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationPreferences",
                schema: "Notification");

            migrationBuilder.DropTable(
                name: "NotificationWork",
                schema: "Notification");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "Notification");
        }
    }
}
