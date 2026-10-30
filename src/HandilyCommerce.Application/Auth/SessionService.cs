using System.Security.Cryptography;
using System.Text;
using HandilyCommerce.Domain.Auth;

namespace HandilyCommerce.Application.Auth;

/// <summary>
/// Use case: admin refresh session with rotation. Reusing a revoked token revokes every session of that user.
/// </summary>
public sealed class SessionService(
    AuthOptions options,
    IRefreshTokenRepository repository,
    IAccessTokenIssuer tokenIssuer,
    TimeProvider timeProvider) : ISessionPort
{
    public IssuedRefreshToken Start(string email) => Create(email, timeProvider.GetUtcNow()).Issued;

    public RefreshResult Refresh(string? token)
    {
        var current = Find(token);
        if (current is null)
        {
            return RefreshResult.Unauthorized();
        }

        var now = timeProvider.GetUtcNow();
        if (current.RevokedAtUtc is not null)
        {
            repository.RevokeAll(current.Email, now);
            return RefreshResult.Unauthorized();
        }

        if (current.ExpiresAtUtc <= now)
        {
            return RefreshResult.Unauthorized();
        }

        var (next, issued) = Create(current.Email, now);
        repository.Update(current with { RevokedAtUtc = now, ReplacedById = next.Id });
        return new RefreshResult(tokenIssuer.Issue(current.Email), issued);
    }

    public void Logout(string? token)
    {
        var current = Find(token);
        if (current is { RevokedAtUtc: null })
        {
            repository.Update(current with { RevokedAtUtc = timeProvider.GetUtcNow() });
        }
    }

    public static string HashToken(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private RefreshToken? Find(string? token) =>
        string.IsNullOrWhiteSpace(token) ? null : repository.FindByHash(HashToken(token));

    private (RefreshToken Row, IssuedRefreshToken Issued) Create(string email, DateTimeOffset now)
    {
        var raw = Base64Url(RandomNumberGenerator.GetBytes(32));
        var expires = now.AddDays(options.RefreshDays);
        var row = new RefreshToken(Guid.NewGuid(), HashToken(raw), email, now, expires);
        repository.Add(row);
        return (row, new IssuedRefreshToken(raw, expires));
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
