using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Search.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Init2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:Search.btree_gin", ",,")
                .Annotation("Npgsql:PostgresExtension:Search.pg_trgm", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:btree_gin", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gin", ",,")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:Search.btree_gin", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:Search.pg_trgm", ",,");
        }
    }
}
