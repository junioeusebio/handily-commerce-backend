using System.Security.Claims;
using System.Text;
using HandilyCommerce.Application.Auth;
using HandilyCommerce.Domain.Auth;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace HandilyCommerce.Infrastructure.Auth;

/// <summary>Issues HS256 JWTs with the e-mail claim, signed with <see cref="AuthOptions.JwtKey"/>.</summary>
public sealed class JwtAccessTokenIssuer(AuthOptions options, TimeProvider timeProvider) : IAccessTokenIssuer
{
    public AccessToken Issue(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        if (string.IsNullOrWhiteSpace(options.JwtKey))
        {
            throw new InvalidOperationException("Auth:JwtKey is not configured.");
        }

        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(options.ExpiresMinutes);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            ]),
            SigningCredentials = new SigningCredentials(SigningKey(options.JwtKey), SecurityAlgorithms.HmacSha256),
        };

        return new AccessToken(new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }

    public static SymmetricSecurityKey SigningKey(string jwtKey) => new(Encoding.UTF8.GetBytes(jwtKey));
}
