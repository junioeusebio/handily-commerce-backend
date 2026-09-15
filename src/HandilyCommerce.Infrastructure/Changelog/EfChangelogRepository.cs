using HandilyCommerce.Domain.Changelog;
using HandilyCommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HandilyCommerce.Infrastructure.Changelog;

/// <summary>
/// EF Core adapter for <see cref="IChangelogRepository"/> against Supabase Postgres.
/// Domain/Application ports stay unchanged; JSON file remains only for the merge workflow seed/append until a later PR writes to the DB.
/// </summary>
public sealed class EfChangelogRepository(HandilyCommerceDbContext dbContext) : IChangelogRepository
{
    public IReadOnlyList<ChangelogEntry> ListAll() =>
        dbContext.ChangelogEntries
            .AsNoTracking()
            .ToList();
}
