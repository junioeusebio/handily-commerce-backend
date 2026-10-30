using HandilyCommerce.Domain.Auth;
using HandilyCommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HandilyCommerce.Infrastructure.Auth;

/// <summary>EF Core adapter for <see cref="IRefreshTokenRepository"/> (<c>RefreshTokens</c>).</summary>
public sealed class EfRefreshTokenRepository(HandilyCommerceDbContext dbContext) : IRefreshTokenRepository
{
    public RefreshToken? FindByHash(string tokenHash) =>
        dbContext.RefreshTokens.AsNoTracking().FirstOrDefault(t => t.TokenHash == tokenHash);

    public void Add(RefreshToken token)
    {
        dbContext.RefreshTokens.Add(token);
        dbContext.SaveChanges();
        dbContext.ChangeTracker.Clear();
    }

    public void Update(RefreshToken token)
    {
        dbContext.RefreshTokens.Update(token);
        dbContext.SaveChanges();
        dbContext.ChangeTracker.Clear();
    }

    public int RevokeAll(string email, DateTimeOffset now)
    {
        var active = dbContext.RefreshTokens.AsNoTracking()
            .Where(t => t.Email == email && t.RevokedAtUtc == null)
            .ToList();
        foreach (var token in active)
        {
            dbContext.RefreshTokens.Update(token with { RevokedAtUtc = now });
        }

        dbContext.SaveChanges();
        dbContext.ChangeTracker.Clear();
        return active.Count;
    }
}
