using HandilyCommerce.Domain.Leads;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HandilyCommerce.Infrastructure.Leads;

/// <summary>EF mapping for <see cref="Lead"/> → <c>Leads</c>.</summary>
public static class LeadModel
{
    public const string TableName = "Leads";

    public static void Configure(EntityTypeBuilder<Lead> entity)
    {
        entity.ToTable(TableName);
        entity.HasKey(l => l.Id);
        entity.Property(l => l.Name).IsRequired().HasMaxLength(120);
        entity.Property(l => l.Email).IsRequired().HasMaxLength(254);
        entity.Property(l => l.Municipality).IsRequired().HasMaxLength(120);
        entity.Property(l => l.Message).HasMaxLength(2000);
        entity.Property(l => l.CreatedAt).IsRequired();
        entity.HasIndex(l => l.CreatedAt);
    }
}
