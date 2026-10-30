using HandilyCommerce.Domain.Auth;
using HandilyCommerce.Infrastructure.Auth;
using HandilyCommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HandilyCommerce.Infrastructure.Tests.Auth;

/// <summary>Uses EF InMemory — no live Supabase / Postgres required in CI.</summary>
public class EfRefreshTokenRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    private static HandilyCommerceDbContext Context(string name) =>
        new(new DbContextOptionsBuilder<HandilyCommerceDbContext>().UseInMemoryDatabase(name).Options);

    private static RefreshToken Token(string hash, string email = "a@x.com") =>
        new(Guid.NewGuid(), hash, email, Now, Now.AddDays(7));

    [Fact]
    public void AddFindUpdate_RoundTrips()
    {
        using var context = Context(nameof(AddFindUpdate_RoundTrips));
        var sut = new EfRefreshTokenRepository(context);
        var token = Token("h1");

        sut.Add(token);
        sut.Update(token with { RevokedAtUtc = Now });

        Assert.Equal(Now, sut.FindByHash("h1")!.RevokedAtUtc);
        Assert.Null(sut.FindByHash("nada"));
    }

    [Fact]
    public void RevokeAll_RevokesOnlyActiveOfThatEmail()
    {
        using var context = Context(nameof(RevokeAll_RevokesOnlyActiveOfThatEmail));
        var sut = new EfRefreshTokenRepository(context);
        sut.Add(Token("a1"));
        sut.Add(Token("a2"));
        sut.Add(Token("a3") with { RevokedAtUtc = Now.AddDays(-1) });
        sut.Add(Token("b1", "b@x.com"));

        Assert.Equal(2, sut.RevokeAll("a@x.com", Now));
        Assert.Equal(Now, sut.FindByHash("a2")!.RevokedAtUtc);
        Assert.Equal(Now.AddDays(-1), sut.FindByHash("a3")!.RevokedAtUtc);
        Assert.Null(sut.FindByHash("b1")!.RevokedAtUtc);
    }

    [Fact]
    public void Model_MapsRefreshTokensTable()
    {
        using var context = Context(nameof(Model_MapsRefreshTokensTable));

        var entity = context.Model.FindEntityType(typeof(RefreshToken))!;

        Assert.Equal(RefreshTokenModel.TableName, entity.GetTableName());
        Assert.Equal(64, entity.FindProperty(nameof(RefreshToken.TokenHash))!.GetMaxLength());
    }
}
