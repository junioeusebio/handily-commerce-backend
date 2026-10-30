namespace HandilyCommerce.Domain.Auth;

/// <summary>
/// Admin refresh session (table <c>RefreshTokens</c>). Only a hash of the opaque token is stored, never the token.
/// </summary>
public sealed record RefreshToken(
    Guid Id,
    string TokenHash,
    string Email,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? RevokedAtUtc = null,
    Guid? ReplacedById = null)
{
    public bool IsActive(DateTimeOffset now) => RevokedAtUtc is null && ExpiresAtUtc > now;
}
