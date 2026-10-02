using HandilyCommerce.Infrastructure.Persistence;
using Npgsql;

namespace HandilyCommerce.Infrastructure.Tests.Persistence;

public class PostgresConnectionStringTests
{
    [Fact]
    public void ForPooler_TransactionPoolerPort_EnablesNoResetOnClose()
    {
        var result = PostgresConnectionString.ForPooler(
            "Host=aws-0-sa-east-1.pooler.supabase.com;Port=6543;Database=postgres;Username=u;Password=\"p@ss;word\";SSL Mode=Require");

        var builder = new NpgsqlConnectionStringBuilder(result);
        Assert.True(builder.NoResetOnClose);
        Assert.Equal(6543, builder.Port);
        Assert.Equal("p@ss;word", builder.Password);
        Assert.Equal(SslMode.Require, builder.SslMode);
    }

    [Theory]
    [InlineData("Host=aws-0-sa-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=u;Password=p")]
    [InlineData("Host=localhost;Database=postgres;Username=u;Password=p")]
    [InlineData("Host=h;Port=6543;Database=postgres;Username=u;Password=p;No Reset On Close=true")]
    public void ForPooler_OtherPortsOrAlreadySet_ReturnsUnchanged(string connectionString)
    {
        Assert.Equal(connectionString, PostgresConnectionString.ForPooler(connectionString));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ForPooler_Empty_ReturnsAsIs(string? connectionString)
    {
        Assert.Equal(connectionString, PostgresConnectionString.ForPooler(connectionString));
    }
}
