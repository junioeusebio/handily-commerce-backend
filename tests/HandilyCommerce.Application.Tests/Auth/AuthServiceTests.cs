using HandilyCommerce.Application.Auth;
using HandilyCommerce.Domain.Auth;

namespace HandilyCommerce.Application.Tests.Auth;

public class AuthServiceTests
{
    private static readonly AccessToken Token = new("jwt", new DateTimeOffset(2026, 10, 26, 13, 0, 0, TimeSpan.Zero));

    private static AuthOptions Configured() => new()
    {
        AdminEmail = "admin@ajksys.test",
        AdminPasswordHash = "hash",
        JwtKey = new string('k', 32),
    };

    private static AuthService Create(AuthOptions options) =>
        new(options, new FakeVerifier(), new FakeIssuer());

    [Fact]
    public void Login_WithValidCredentials_ReturnsToken()
    {
        var result = Create(Configured()).Login(new LoginCommand(" ADMIN@ajksys.test ", "secret"));

        Assert.Equal(LoginStatus.Success, result.Status);
        Assert.Same(Token, result.Token);
    }

    [Fact]
    public void Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var result = Create(Configured()).Login(new LoginCommand("admin@ajksys.test", "wrong"));

        Assert.Equal(LoginStatus.Unauthorized, result.Status);
        Assert.Null(result.Token);
    }

    [Fact]
    public void Login_WithWrongEmail_ReturnsUnauthorized()
    {
        var result = Create(Configured()).Login(new LoginCommand("x@y.com", "secret"));

        Assert.Equal(LoginStatus.Unauthorized, result.Status);
    }

    [Fact]
    public void Login_WithMissingFields_ReturnsInvalid()
    {
        var result = Create(Configured()).Login(new LoginCommand(" ", null));

        Assert.Equal(LoginStatus.Invalid, result.Status);
        Assert.Contains("email", result.Errors.Keys);
        Assert.Contains("password", result.Errors.Keys);
    }

    [Fact]
    public void Login_WhenNotConfigured_ReturnsDisabled()
    {
        var result = Create(new AuthOptions()).Login(new LoginCommand("a@b.com", "secret"));

        Assert.Equal(LoginStatus.Disabled, result.Status);
    }

    [Fact]
    public void Validate_WithShortKey_Throws()
    {
        var options = Configured();
        options.JwtKey = "short";

        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void Validate_WithBadExpiry_Throws()
    {
        var options = Configured();
        options.ExpiresMinutes = 0;

        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void Validate_WhenNotConfiguredOrValid_DoesNotThrow()
    {
        new AuthOptions { JwtKey = "short" }.Validate();
        Configured().Validate();
    }

    private sealed class FakeVerifier : IPasswordVerifier
    {
        public bool Verify(string password, string passwordHash) => password == "secret";
    }

    private sealed class FakeIssuer : IAccessTokenIssuer
    {
        public AccessToken Issue(string email) => Token;
    }
}
