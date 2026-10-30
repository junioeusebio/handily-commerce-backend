using HandilyCommerce.Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HandilyCommerce.Infrastructure.Auth;

/// <summary>EF mapping for <see cref="RefreshToken"/> → <c>RefreshTokens</c>.</summary>
public static class RefreshTokenModel
{
    public const string TableName = "RefreshTokens";

    public static void Configure(EntityTypeBuilder<RefreshToken> entity)
    {
        entity.ToTable(TableName);
        entity.HasKey(e => e.Id);
        entity.Property(e => e.TokenHash).IsRequired().HasMaxLength(64);
        entity.Property(e => e.Email).IsRequired().HasMaxLength(254);
        entity.Property(e => e.CreatedAtUtc).IsRequired();
        entity.Property(e => e.ExpiresAtUtc).IsRequired();
        entity.HasIndex(e => e.TokenHash).IsUnique();
        entity.HasIndex(e => e.Email);
    }
}
