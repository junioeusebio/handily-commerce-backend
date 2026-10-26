using HandilyCommerce.Domain.Auth;

namespace HandilyCommerce.Domain.Tests.Auth;

public class LoginResultTests
{
    [Fact]
    public void Factories_SetStatus()
    {
        var token = new AccessToken("t", DateTimeOffset.UnixEpoch);

        Assert.Equal(LoginStatus.Success, LoginResult.Success(token).Status);
        Assert.Equal(LoginStatus.Unauthorized, LoginResult.Unauthorized().Status);
        Assert.Equal(LoginStatus.Disabled, LoginResult.Disabled().Status);
        Assert.Equal(LoginStatus.Invalid, LoginResult.Invalid(new Dictionary<string, string[]> { ["email"] = ["x"] }).Status);
        Assert.Equal("a", new LoginCommand("a", "b").Email);
    }
}
