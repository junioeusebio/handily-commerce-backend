using HandilyCommerce.Domain.Changelog;
using Microsoft.EntityFrameworkCore;

namespace HandilyCommerce.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for Supabase Postgres (Npgsql). Maps domain models 1:1 where possible.
/// </summary>
public sealed class HandilyCommerceDbContext(DbContextOptions<HandilyCommerceDbContext> options)
    : DbContext(options)
{
    public DbSet<ChangelogEntry> ChangelogEntries => Set<ChangelogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChangelogEntry>(entity =>
        {
            entity.ToTable("ChangelogEntries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Summary).IsRequired().HasMaxLength(4000);
            entity.Property(e => e.ProductVersion).HasMaxLength(64);
            entity.Property(e => e.Label).HasMaxLength(64);

            // Seed from existing changelog.json (#11, #10, #7) — same deterministic Guids.
            entity.HasData(
                new ChangelogEntry(
                    Guid.Parse("a10c0000-0011-4000-8000-00000000000b"),
                    "feat: changelog API for Major/Mirror releases",
                    "Changelog API for the FE \"What's new\" modal: hexagonal ports, GET /api/v1/changelog from embedded JSON seed, and merge workflow to append Major/Mirror entries.",
                    "0.3.0",
                    DateTimeOffset.Parse("2026-09-10T22:44:17Z"),
                    11,
                    "Release Mirror"),
                new ChangelogEntry(
                    Guid.Parse("a10c0000-0010-4000-8000-00000000000a"),
                    "feat(A2): Scalar OpenAPI UI",
                    "Adds Scalar UI over the existing OpenAPI document in all environments, including Production on Render.",
                    "0.2.0",
                    DateTimeOffset.Parse("2026-09-09T14:39:41Z"),
                    10,
                    "Release Mirror"),
                new ChangelogEntry(
                    Guid.Parse("a10c0000-0007-4000-8000-000000000007"),
                    "chore: Versioning 0.1.0 + Release labels process",
                    "Introduces product Versioning separate from the API route version, with Directory.Build.props baseline 0.1.0 and the Release label process.",
                    "0.1.0",
                    DateTimeOffset.Parse("2026-09-09T00:41:37Z"),
                    7,
                    "Release Mirror"));
        });
    }
}
