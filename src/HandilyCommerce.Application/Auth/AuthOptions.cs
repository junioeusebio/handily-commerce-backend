namespace HandilyCommerce.Application.Auth;

/// <summary>Config section <c>Auth</c> (env: Auth__AdminEmail, Auth__AdminPasswordHash, Auth__JwtKey, ...).</summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";
    public const int MinJwtKeyBytes = 32;

    public string? AdminEmail { get; set; }

    /// <summary>Format <c>pbkdf2-sha256$iterations$saltBase64$hashBase64</c>.</summary>
    public string? AdminPasswordHash { get; set; }

    public string? JwtKey { get; set; }

    public string Issuer { get; set; } = "handily-commerce-backend";

    public string Audience { get; set; } = "handily-commerce-frontend";

    public int ExpiresMinutes { get; set; } = 60;

    /// <summary>Admin session renewal window in days (default 7).</summary>
    public int RefreshDays { get; set; } = 7;

    /// <summary>Login is enabled only when admin e-mail, password hash and JWT key are all set.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AdminEmail)
        && !string.IsNullOrWhiteSpace(AdminPasswordHash)
        && !string.IsNullOrWhiteSpace(JwtKey);

    /// <summary>Startup check (only when configured): key length and expiry range.</summary>
    public void Validate()
    {
        if (!IsConfigured)
        {
            return;
        }

        if (System.Text.Encoding.UTF8.GetByteCount(JwtKey!) < MinJwtKeyBytes)
        {
            throw new InvalidOperationException($"Auth:JwtKey must have at least {MinJwtKeyBytes} bytes.");
        }

        if (ExpiresMinutes is < 1 or > 1440)
        {
            throw new InvalidOperationException("Auth:ExpiresMinutes must be between 1 and 1440.");
        }

        if (RefreshDays is < 1 or > 90)
        {
            throw new InvalidOperationException("Auth:RefreshDays must be between 1 and 90.");
        }
    }
}
