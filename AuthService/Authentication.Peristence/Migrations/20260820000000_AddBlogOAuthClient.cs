using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authentication.Peristence.Migrations;

/// <inheritdoc />
[DbContext(typeof(AuthenticationDbContext))]
[Migration("20260820000000_AddBlogOAuthClient")]
public partial class AddBlogOAuthClient : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            INSERT INTO "Authentication"."Clients" ("ClientId", "RedirectUri")
            VALUES ('blog', 'http://localhost:3000/callback')
            ON CONFLICT ("ClientId") DO UPDATE
            SET "RedirectUri" = EXCLUDED."RedirectUri";
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM "Authentication"."Clients"
            WHERE "ClientId" = 'blog';
            """);
    }
}
