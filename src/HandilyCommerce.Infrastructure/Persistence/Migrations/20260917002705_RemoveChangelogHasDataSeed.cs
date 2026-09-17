using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandilyCommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Stops the model from owning ChangelogEntries seed data (HasData).
    /// Intentionally empty: production Supabase already has those rows; do not DeleteData.
    /// </remarks>
    public partial class RemoveChangelogHasDataSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: model/snapshot no longer includes HasData seed.
            // Do not delete existing ChangelogEntries rows in Supabase.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: do not re-insert seed via migration; data remains in the database.
        }
    }
}
