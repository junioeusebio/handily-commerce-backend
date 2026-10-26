namespace HandilyCommerce.Domain.Auth;

/// <summary>Signed access token (JWT) and its expiry.</summary>
public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);
