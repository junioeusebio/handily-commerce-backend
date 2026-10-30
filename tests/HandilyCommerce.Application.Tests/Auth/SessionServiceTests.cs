using HandilyCommerce.Application.Auth;
using HandilyCommerce.Domain.Auth;

namespace HandilyCommerce.Application.Tests.Auth;

public class SessionServiceTests
{
    private const string Email = "admin@ajksys.test";
    private readonly FakeRepo _repo = new();
    private readonly MutableTime _time = new(new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero));

    private SessionService Sut(int days = 7) =>
        new(new AuthOptions { RefreshDays = days }, _repo, new FakeIssuer(), _time);

    [Fact]
    public void Start_StoresOnlyHashAndUsesRefreshDays()
    {
        var issued = Sut(3).Start(Email);

        var row = Assert.Single(_repo.Rows.Values);
        Assert.NotEqual(issued.Token, row.TokenHash);
        Assert.Equal(SessionService.HashToken(issued.Token), row.TokenHash);
        Assert.Equal(64, row.TokenHash.Length);
        Assert.Equal(_time.GetUtcNow().AddDays(3), issued.ExpiresAt);
        Assert.True(issued.Token.Length >= 43);
    }

    [Fact]
    public void Refresh_RotatesAndLinksReplacement()
    {
        var sut = Sut();
        var first = sut.Start(Email);

        var result = sut.Refresh(first.Token);

        Assert.True(result.IsSuccess);
        var old = _repo.FindByHash(SessionService.HashToken(first.Token))!;
        Assert.NotNull(old.RevokedAtUtc);
        Assert.Equal(_repo.FindByHash(SessionService.HashToken(result.RefreshToken!.Token))!.Id, old.ReplacedById);
    }

    [Fact]
    public void Refresh_Expired_IsUnauthorized()
    {
        var sut = Sut(1);
        var issued = sut.Start(Email);
        _time.Now = _time.Now.AddDays(2);

        Assert.False(sut.Refresh(issued.Token).IsSuccess);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("desconhecido")]
    public void Refresh_MissingOrUnknown_IsUnauthorized(string? token) =>
        Assert.False(Sut().Refresh(token).IsSuccess);

    [Fact]
    public void Refresh_ReuseOfRevoked_RevokesAllActive()
    {
        var sut = Sut();
        var first = sut.Start(Email);
        var rotated = sut.Refresh(first.Token);

        Assert.False(sut.Refresh(first.Token).IsSuccess);
        Assert.False(sut.Refresh(rotated.RefreshToken!.Token).IsSuccess);
        Assert.All(_repo.Rows.Values, r => Assert.NotNull(r.RevokedAtUtc));
    }

    [Fact]
    public void Logout_RevokesOnceAndIgnoresUnknown()
    {
        var sut = Sut();
        var issued = sut.Start(Email);

        sut.Logout(issued.Token);
        var revokedAt = _repo.Rows.Values.Single().RevokedAtUtc;
        _time.Now = _time.Now.AddMinutes(5);
        sut.Logout(issued.Token);
        sut.Logout(null);

        Assert.NotNull(revokedAt);
        Assert.Equal(revokedAt, _repo.Rows.Values.Single().RevokedAtUtc);
        Assert.False(sut.Refresh(issued.Token).IsSuccess);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(91)]
    public void Options_RefreshDaysOutOfRange_Throws(int days)
    {
        var options = new AuthOptions
        {
            AdminEmail = Email, AdminPasswordHash = "x", JwtKey = new string('k', 32), RefreshDays = days,
        };

        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    private sealed class MutableTime(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class FakeIssuer : IAccessTokenIssuer
    {
        public AccessToken Issue(string email) => new("jwt-" + email, DateTimeOffset.UnixEpoch);
    }

    private sealed class FakeRepo : IRefreshTokenRepository
    {
        public Dictionary<Guid, RefreshToken> Rows { get; } = [];

        public RefreshToken? FindByHash(string tokenHash) => Rows.Values.FirstOrDefault(r => r.TokenHash == tokenHash);

        public void Add(RefreshToken token) => Rows[token.Id] = token;

        public void Update(RefreshToken token) => Rows[token.Id] = token;

        public int RevokeAll(string email, DateTimeOffset now)
        {
            var active = Rows.Values.Where(r => r.Email == email && r.RevokedAtUtc is null).ToList();
            active.ForEach(r => Rows[r.Id] = r with { RevokedAtUtc = now });
            return active.Count;
        }
    }
}
