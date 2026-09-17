using HandilyCommerce.Domain.Changelog;
using HandilyCommerce.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace HandilyCommerce.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for Supabase Postgres (Npgsql). Maps domain models 1:1 where possible.
/// </summary>
public sealed class HandilyCommerceDbContext(DbContextOptions<HandilyCommerceDbContext> options)
    : DbContext(options)
{
    public DbSet<ChangelogEntry> ChangelogEntries => Set<ChangelogEntry>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Item> Items => Set<Item>();

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
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Sku).HasMaxLength(64);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.HasIndex(e => e.Sku).IsUnique().HasFilter("\"Sku\" IS NOT NULL");

            entity.HasMany(e => e.Items)
                .WithOne()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.ToTable("Items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.HasIndex(e => e.ProductId);
        });
    }
}
