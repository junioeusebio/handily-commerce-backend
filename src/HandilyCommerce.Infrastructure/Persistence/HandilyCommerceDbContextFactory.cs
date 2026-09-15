using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HandilyCommerce.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for <c>dotnet ef migrations</c>. Uses a local placeholder connection string
/// (never a real secret). Runtime prefers Supabase Transaction pooler via <c>ConnectionStrings:Default</c>.
/// </summary>
public sealed class HandilyCommerceDbContextFactory : IDesignTimeDbContextFactory<HandilyCommerceDbContext>
{
    public HandilyCommerceDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HandilyCommerceDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=PLACEHOLDER;SSL Mode=Prefer");
        return new HandilyCommerceDbContext(optionsBuilder.Options);
    }
}
