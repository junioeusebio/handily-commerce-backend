namespace HandilyCommerce.Domain.Auth;

/// <summary>Driven port: persistence of <see cref="RefreshToken"/> rows (adapter: EF Core).</summary>
public interface IRefreshTokenRepository
{
    RefreshToken? FindByHash(string tokenHash);

    void Add(RefreshToken token);

    void Update(RefreshToken token);

    /// <summary>Revokes every non-revoked token of <paramref name="email"/>; returns how many.</summary>
    int RevokeAll(string email, DateTimeOffset now);
}
