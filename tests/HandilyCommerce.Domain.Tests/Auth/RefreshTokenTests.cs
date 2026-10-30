using HandilyCommerce.Domain.Auth;

namespace HandilyCommerce.Domain.Tests.Auth;

public class RefreshTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IsActive_FalseWhenRevokedOrExpired()
    {
        var token = new RefreshToken(Guid.NewGuid(), "h", "a@x.com", Now, Now.AddDays(1));

        Assert.True(token.IsActive(Now));
        Assert.False(token.IsActive(Now.AddDays(2)));
        Assert.False((token with { RevokedAtUtc = Now }).IsActive(Now));
    }

    [Fact]
    public void RefreshResult_Unauthorized_IsNotSuccess()
    {
        Assert.False(RefreshResult.Unauthorized().IsSuccess);
        Assert.True(new RefreshResult(new AccessToken("t", Now), new IssuedRefreshToken("r", Now)).IsSuccess);
    }

    [Fact]
    public void LoginSuccess_CarriesEmail() =>
        Assert.Equal("a@x.com", LoginResult.Success(new AccessToken("t", Now), "a@x.com").Email);
}
