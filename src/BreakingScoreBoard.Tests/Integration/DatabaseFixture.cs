using BreakingScoreBoard.Api.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace BreakingScoreBoard.Tests.Integration;

/// <summary>
/// Test fixture that provides a PostgreSQL database and configured WebApplicationFactory.
/// Uses the existing PostgreSQL instance from the dev container.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    private const string TestDatabaseName = "breakingscoreboard_test";
    private const string MasterConnectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";

    /// <summary>
    /// Gets the connection string for the test database.
    /// </summary>
    public string ConnectionString => $"Host=localhost;Port=5432;Database={TestDatabaseName};Username=postgres;Password=postgres";

    /// <summary>
    /// Initializes the test database by creating it if it doesn't exist.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Create the test database if it doesn't exist
        await using var masterConnection = new NpgsqlConnection(MasterConnectionString);
        await masterConnection.OpenAsync();

        // Check if database exists
        await using var checkCmd = new NpgsqlCommand(
            $"SELECT 1 FROM pg_database WHERE datname = '{TestDatabaseName}'",
            masterConnection);
        var exists = await checkCmd.ExecuteScalarAsync();

        if (exists == null)
        {
            // Create database
            await using var createCmd = new NpgsqlCommand(
                $"CREATE DATABASE {TestDatabaseName}",
                masterConnection);
            await createCmd.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Cleans up the test database.
    /// </summary>
    public async Task DisposeAsync()
    {
        // Drop all tables to clean up for next test run
        await using var testConnection = new NpgsqlConnection(ConnectionString);
        await testConnection.OpenAsync();

        await using var cmd = new NpgsqlCommand(@"
            DO $$ DECLARE
                r RECORD;
            BEGIN
                FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = 'public') LOOP
                    EXECUTE 'DROP TABLE IF EXISTS ' || quote_ident(r.tablename) || ' CASCADE';
                END LOOP;
            END $$;", testConnection);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates a new DbContext instance for direct database access in tests.
    /// </summary>
    public BattleDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BattleDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new BattleDbContext(options);
    }
}

/// <summary>
/// WebApplicationFactory configured to use Testcontainers PostgreSQL.
/// </summary>
public class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly DatabaseFixture _databaseFixture;

    public IntegrationTestFactory()
    {
        _databaseFixture = new DatabaseFixture();
    }

    /// <summary>
    /// Gets the connection string for the test database.
    /// </summary>
    public string ConnectionString => _databaseFixture.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<BattleDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add DbContext with test container connection string
            services.AddDbContext<BattleDbContext>(options =>
                options.UseNpgsql(_databaseFixture.ConnectionString));
        });

        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        await _databaseFixture.InitializeAsync();

        // Apply migrations
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BattleDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _databaseFixture.DisposeAsync();
    }
}

/// <summary>
/// Collection definition for sharing DatabaseFixture across tests.
/// </summary>
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<IntegrationTestFactory>
{
}
