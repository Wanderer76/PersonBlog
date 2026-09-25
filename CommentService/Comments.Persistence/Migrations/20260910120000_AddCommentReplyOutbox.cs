using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comments.Persistence.Migrations;

[DbContext(typeof(CommentDbContext))]
[Migration("20260910120000_AddCommentReplyOutbox")]
public sealed class AddCommentReplyOutbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "OutboxMessages",
            schema: "Comment",
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
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropTable(name: "OutboxMessages", schema: "Comment");
}
