using HandilyCommerce.Application.Auth;
using HandilyCommerce.Infrastructure.Auth;
using Microsoft.IdentityModel.JsonWebTokens;

namespace HandilyCommerce.Infrastructure.Tests.Auth;

public class AuthAdaptersTests
{
    private readonly Pbkdf2PasswordVerifier _verifier = new();

    [Fact]
    public void Verify_WithMatchingPassword_ReturnsTrue()
    {
        var hash = Pbkdf2PasswordVerifier.Hash("s3nha", iterations: 1000);

        Assert.True(_verifier.Verify("s3nha", hash));
        Assert.False(_verifier.Verify("outra", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain")]
    [InlineData("md5$1000$c2FsdA==$aGFzaA==")]
    [InlineData("pbkdf2-sha256$abc$c2FsdA==$aGFzaA==")]
    [InlineData("pbkdf2-sha256$0$c2FsdA==$aGFzaA==")]
    [InlineData("pbkdf2-sha256$1000$not-base64!$aGFzaA==")]
    [InlineData("pbkdf2-sha256$1000$$aGFzaA==")]
    public void Verify_WithMalformedHash_ReturnsFalse(string hash)
    {
        Assert.False(_verifier.Verify("x", hash));
    }

    [Fact]
    public void Issue_CreatesHs256TokenWithEmailAndExpiry()
    {
        var now = new DateTimeOffset(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);
        var options = new AuthOptions { JwtKey = new string('k', 32), ExpiresMinutes = 30 };
        var sut = new JwtAccessTokenIssuer(options, new FixedTime(now));

        var token = sut.Issue("admin@ajksys.test");
        var jwt = new JsonWebToken(token.Token);

        Assert.Equal(now.AddMinutes(30), token.ExpiresAt);
        Assert.Equal("HS256", jwt.Alg);
        Assert.Equal("admin@ajksys.test", jwt.GetClaim("email").Value);
        Assert.Equal(options.Issuer, jwt.Issuer);
        Assert.Contains(options.Audience, jwt.Audiences);
    }

    [Fact]
    public void Issue_WithoutKey_Throws()
    {
        var sut = new JwtAccessTokenIssuer(new AuthOptions(), TimeProvider.System);

        Assert.Throws<InvalidOperationException>(() => sut.Issue("a@b.com"));
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
