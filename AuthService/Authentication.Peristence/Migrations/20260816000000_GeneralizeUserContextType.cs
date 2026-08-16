using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authentication.Peristence.Migrations;

/// <inheritdoc />
[DbContext(typeof(AuthenticationDbContext))]
[Migration("20260816000000_GeneralizeUserContextType")]
public partial class GeneralizeUserContextType : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(
            name: "PK_UserContexts",
            schema: "Authentication",
            table: "UserContexts");

        migrationBuilder.AddColumn<string>(
            name: "ContextTypeValue",
            schema: "Authentication",
            table: "UserContexts",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "");

        migrationBuilder.Sql(
            """
            UPDATE "Authentication"."UserContexts"
            SET "ContextTypeValue" = CASE "ContextType"
                WHEN 0 THEN 'blog'
                WHEN 1 THEN 'artist'
                ELSE 'legacy-' || "ContextType"::text
            END;
            """);

        migrationBuilder.DropColumn(
            name: "ContextType",
            schema: "Authentication",
            table: "UserContexts");

        migrationBuilder.RenameColumn(
            name: "ContextTypeValue",
            schema: "Authentication",
            table: "UserContexts",
            newName: "ContextType");

        migrationBuilder.AddPrimaryKey(
            name: "PK_UserContexts",
            schema: "Authentication",
            table: "UserContexts",
            columns: new[] { "UserId", "ContextType", "ContextId" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(
            name: "PK_UserContexts",
            schema: "Authentication",
            table: "UserContexts");

        migrationBuilder.AddColumn<int>(
            name: "ContextTypeValue",
            schema: "Authentication",
            table: "UserContexts",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.Sql(
            """
            UPDATE "Authentication"."UserContexts"
            SET "ContextTypeValue" = CASE "ContextType"
                WHEN 'artist' THEN 1
                ELSE 0
            END;
            """);

        migrationBuilder.DropColumn(
            name: "ContextType",
            schema: "Authentication",
            table: "UserContexts");

        migrationBuilder.RenameColumn(
            name: "ContextTypeValue",
            schema: "Authentication",
            table: "UserContexts",
            newName: "ContextType");

        migrationBuilder.AddPrimaryKey(
            name: "PK_UserContexts",
            schema: "Authentication",
            table: "UserContexts",
            columns: new[] { "UserId", "ContextType", "ContextId" });
    }
}
