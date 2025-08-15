using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoReacting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SubscribedChanels_UserId_BlogId",
                schema: "VideoReacting",
                table: "SubscribedChanels",
                columns: new[] { "UserId", "BlogId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubscribedChanels_UserId_BlogId",
                schema: "VideoReacting",
                table: "SubscribedChanels");
        }
    }
}
