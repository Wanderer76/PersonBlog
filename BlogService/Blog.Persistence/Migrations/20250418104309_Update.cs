using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Profile.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_SubscriptionLevels_SubscriptionId",
                schema: "Profile",
                table: "Posts");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfileSubscriptions_SubscriptionLevels_SubscriptionLevelId",
                schema: "Profile",
                table: "ProfileSubscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoMetadata_Posts_PostId",
                schema: "Profile",
                table: "VideoMetadata");

            migrationBuilder.DropTable(
                name: "SubscriptionLevels",
                schema: "Profile");

            migrationBuilder.DropIndex(
                name: "IX_Posts_SubscriptionId",
                schema: "Profile",
                table: "Posts");

            migrationBuilder.RenameColumn(
                name: "SubscriptionId",
                schema: "Profile",
                table: "Posts",
                newName: "PaymentSubscriptionId");

            migrationBuilder.CreateTable(
                name: "PaymentSubscriptions",
                schema: "Profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlogId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<double>(type: "double precision", nullable: false),
                    ImageId = table.Column<string>(type: "text", nullable: true),
                    NextLevelId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentSubscriptions_PaymentSubscriptions_NextLevelId",
                        column: x => x.NextLevelId,
                        principalSchema: "Profile",
                        principalTable: "PaymentSubscriptions",
                        principalColumn: "Id");
                });

            migrationBuilder.UpdateData(
                schema: "Profile",
                table: "Blogs",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 4, 18, 10, 43, 7, 120, DateTimeKind.Unspecified).AddTicks(3843), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSubscriptions_NextLevelId",
                schema: "Profile",
                table: "PaymentSubscriptions",
                column: "NextLevelId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProfileSubscriptions_PaymentSubscriptions_SubscriptionLevel~",
                schema: "Profile",
                table: "ProfileSubscriptions",
                column: "SubscriptionLevelId",
                principalSchema: "Profile",
                principalTable: "PaymentSubscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProfileSubscriptions_PaymentSubscriptions_SubscriptionLevel~",
                schema: "Profile",
                table: "ProfileSubscriptions");

            migrationBuilder.DropTable(
                name: "PaymentSubscriptions",
                schema: "Profile");

            migrationBuilder.RenameColumn(
                name: "PaymentSubscriptionId",
                schema: "Profile",
                table: "Posts",
                newName: "SubscriptionId");

            migrationBuilder.CreateTable(
                name: "SubscriptionLevels",
                schema: "Profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NextLevelId = table.Column<Guid>(type: "uuid", nullable: true),
                    BlogId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ImageId = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Price = table.Column<double>(type: "double precision", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionLevels_SubscriptionLevels_NextLevelId",
                        column: x => x.NextLevelId,
                        principalSchema: "Profile",
                        principalTable: "SubscriptionLevels",
                        principalColumn: "Id");
                });

            migrationBuilder.UpdateData(
                schema: "Profile",
                table: "Blogs",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d3e"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 4, 16, 10, 50, 49, 607, DateTimeKind.Unspecified).AddTicks(7397), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_Posts_SubscriptionId",
                schema: "Profile",
                table: "Posts",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionLevels_NextLevelId",
                schema: "Profile",
                table: "SubscriptionLevels",
                column: "NextLevelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_SubscriptionLevels_SubscriptionId",
                schema: "Profile",
                table: "Posts",
                column: "SubscriptionId",
                principalSchema: "Profile",
                principalTable: "SubscriptionLevels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProfileSubscriptions_SubscriptionLevels_SubscriptionLevelId",
                schema: "Profile",
                table: "ProfileSubscriptions",
                column: "SubscriptionLevelId",
                principalSchema: "Profile",
                principalTable: "SubscriptionLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VideoMetadata_Posts_PostId",
                schema: "Profile",
                table: "VideoMetadata",
                column: "PostId",
                principalSchema: "Profile",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
