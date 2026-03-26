using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authentication.Peristence.Migrations
{
    /// <inheritdoc />
    public partial class AddClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clients",
                schema: "Authentication",
                columns: table => new
                {
                    ClientId = table.Column<string>(type: "text", nullable: false),
                    RedirectUri = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.ClientId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_ClientId_RedirectUri",
                schema: "Authentication",
                table: "Clients",
                columns: new[] { "ClientId", "RedirectUri" },
                unique: true);

            migrationBuilder.Sql(@"
              insert into ""Authentication"".""Clients""(""ClientId"",""RedirectUri"") values
              ('auth','http://localhost:5174');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Clients",
                schema: "Authentication");

            migrationBuilder.UpdateData(
                schema: "Authentication",
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: new Guid("09f3c24e-6e70-48ea-a5c5-60727af95d1e"),
                columns: new[] { "CreatedAt", "Password" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 2, 27, 5, 58, 21, 279, DateTimeKind.Unspecified).AddTicks(871), new TimeSpan(0, 0, 0, 0, 0)), "O0VWS+HeCMg=;R6kqABwX+gHnnmjHT+tNt3Rpb3AbqUItP5EqBkKML8s=" });
        }
    }
}
