using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandilyCommerce.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Enables Row Level Security on public tables (Supabase Security Advisor: rls_disabled_in_public).
    /// No policies: the API connects as the table owner (bypasses RLS); anon/authenticated via PostgREST are denied.
    /// </summary>
    public partial class EnableRowLevelSecurity : Migration
    {
        private static readonly string[] Tables = ["ChangelogEntries", "Products", "Items", "Courses", "Leads"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.Sql($"ALTER TABLE public.\"{table}\" ENABLE ROW LEVEL SECURITY;");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.Sql($"ALTER TABLE public.\"{table}\" DISABLE ROW LEVEL SECURITY;");
            }
        }
    }
}
