using System.Security.Cryptography;
using HandilyCommerce.Application.Auth;
using HandilyCommerce.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace HandilyCommerce.Api.Auth;

/// <summary>
/// JWT bearer wiring. When Auth__* is not configured a random key is used, so protected routes
/// still answer 401 and the app boots without secrets.
/// </summary>
public static class AuthSetup
{
    public static IServiceCollection AddJwtAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var auth = new AuthOptions();
        configuration.GetSection(AuthOptions.SectionName).Bind(auth);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = ValidationParameters(auth);
            });
        services.AddAuthorization();
        return services;
    }

    public static TokenValidationParameters ValidationParameters(AuthOptions auth)
    {
        ArgumentNullException.ThrowIfNull(auth);
        SecurityKey key = auth.IsConfigured
            ? JwtAccessTokenIssuer.SigningKey(auth.JwtKey!)
            : new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64));

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = auth.Issuer,
            ValidateAudience = true,
            ValidAudience = auth.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "email",
        };
    }
}
