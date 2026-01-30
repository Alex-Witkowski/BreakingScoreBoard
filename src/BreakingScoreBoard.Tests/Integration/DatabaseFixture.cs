using BreakingScoreBoard.Api.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace BreakingScoreBoard.Tests.Integration;

/// <summary>
/// Test fixture that provides a PostgreSQL container and configured WebApplicationFactory.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    
    public DatabaseFixture()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("breakingscoreboard_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();
    }
    
    /// <summary>
    /// Gets the connection string for the test database.
    /// </summary>
    public string ConnectionString => _postgresContainer.GetConnectionString();
    
    /// <summary>
    /// Initializes the PostgreSQL container.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
    }
    
    /// <summary>
    /// Stops and disposes the PostgreSQL container.
    /// </summary>
    public async Task DisposeAsync()
    {
        await _postgresContainer.StopAsync();
        await _postgresContainer.DisposeAsync();
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
