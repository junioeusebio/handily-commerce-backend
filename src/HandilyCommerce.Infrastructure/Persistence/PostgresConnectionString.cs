using Npgsql;

namespace HandilyCommerce.Infrastructure.Persistence;

/// <summary>
/// Adjusts the Npgsql connection string for the Supabase Supavisor <b>Transaction pooler</b> (port 6543).
/// In transaction mode, Npgsql's connection reset on pool return (<c>DISCARD ALL</c>, prepended to the next
/// command) makes no sense and makes reused connections hang until the 30s timeout. Npgsql docs require
/// <c>No Reset On Close=true</c> when keeping Npgsql pooling on behind a transaction-mode pooler.
/// Other ports (session pooler / direct 5432, local Postgres) are returned unchanged.
/// </summary>
public static class PostgresConnectionString
{
    public const int TransactionPoolerPort = 6543;

    public static string? ForPooler(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (builder.Port != TransactionPoolerPort || builder.NoResetOnClose)
        {
            return connectionString;
        }

        builder.NoResetOnClose = true;
        return builder.ConnectionString;
    }
}
