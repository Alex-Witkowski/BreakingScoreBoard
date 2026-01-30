# Research: Breaking Battle Management System

**Feature**: 001-breaking-battles  
**Date**: 2026-01-30  
**Purpose**: Resolve technical unknowns and establish best practices before design

## Research Tasks

Based on Technical Context unknowns and technology choices:

1. EF Core 8 + PostgreSQL setup and configuration
2. Blazor Server real-time updates (live scores, countdown timer)
3. Testcontainers for PostgreSQL integration tests
4. PIN-based authentication without user accounts
5. OpenAPI/Swagger generation for ASP.NET Core 8

---

## 1. EF Core 8 + PostgreSQL Configuration

**Decision**: Use Npgsql.EntityFrameworkCore.PostgreSQL provider with code-first migrations

**Rationale**: 
- Npgsql is the official PostgreSQL provider for EF Core, maintained by the Npgsql team
- Code-first migrations align with TDD workflow (schema evolves with domain)
- EF Core 8 has improved performance and JSON column support

**Implementation Pattern**:
```csharp
// Program.cs
builder.Services.AddDbContext<BattleDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// DbContext in Infrastructure/
public class BattleDbContext : DbContext
{
    public DbSet<BattleEvent> Events => Set<BattleEvent>();
    public DbSet<Battle> Battles => Set<Battle>();
    public DbSet<JudgeScore> Scores => Set<JudgeScore>();
    // ... other DbSets
}
```

**Packages Required**:
- `Npgsql.EntityFrameworkCore.PostgreSQL` (8.x)
- `Microsoft.EntityFrameworkCore.Design` (8.x) - for migrations CLI

**Alternatives Considered**:
- Dapper: Rejected - EF Core sufficient for CRUD-heavy app; Dapper adds complexity without perf need
- Database-first: Rejected - doesn't align with TDD; schema should follow domain evolution

---

## 2. Blazor Server Real-Time Updates

**Decision**: Use Blazor Server with SignalR for live score updates and countdown timer

**Rationale**:
- Blazor Server maintains persistent SignalR connection by default
- No additional infrastructure needed for real-time features
- Server-side rendering means no JavaScript countdown implementation
- Spectator view can subscribe to battle state changes automatically

**Implementation Pattern**:
```csharp
// For countdown timer - use System.Timers.Timer with InvokeAsync
private Timer? _countdownTimer;
private int _secondsRemaining = 5;

private void StartCountdown()
{
    _countdownTimer = new Timer(1000);
    _countdownTimer.Elapsed += async (s, e) =>
    {
        _secondsRemaining--;
        await InvokeAsync(StateHasChanged);
        if (_secondsRemaining <= 0) RevealScores();
    };
    _countdownTimer.Start();
}

// For cross-client updates - inject IHubContext or use cascading state
@inject BattleStateService BattleState

protected override void OnInitialized()
{
    BattleState.OnScoreSubmitted += HandleScoreUpdate;
}
```

**Key Considerations**:
- Use `IDisposable` pattern to clean up timer subscriptions
- `StateHasChanged()` must be called on UI thread via `InvokeAsync`
- For spectator broadcast, consider `IHubContext<SpectatorHub>` to push to all clients

**Alternatives Considered**:
- Blazor WebAssembly + SignalR: Rejected - adds complexity; server-rendered sufficient per constitution
- Polling: Rejected - inefficient for <5s update requirement
- External SignalR service: Rejected - Blazor Server includes SignalR; no external dependency needed

---

## 3. Testcontainers for PostgreSQL

**Decision**: Use Testcontainers.PostgreSql for integration tests with real database

**Rationale**:
- Tests against real PostgreSQL behavior (constraints, transactions)
- Disposable containers ensure test isolation
- Aligns with Constitution I. Test-First and III. Observability (realistic test environment)

**Implementation Pattern**:
```csharp
// Test fixture
public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .Build();

    public string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        // Apply migrations
        var options = new DbContextOptionsBuilder<BattleDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using var context = new BattleDbContext(options);
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();
}

// Usage in test class
public class ScoringIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    public ScoringIntegrationTests(DatabaseFixture fixture) => _fixture = fixture;
}
```

**Packages Required**:
- `Testcontainers.PostgreSql` (3.x)
- `Microsoft.AspNetCore.Mvc.Testing` (8.x) - for WebApplicationFactory

**Alternatives Considered**:
- In-memory EF provider: Rejected - doesn't test real PostgreSQL constraints/behavior
- Shared test database: Rejected - test pollution; Testcontainers provides isolation
- SQLite: Rejected - different SQL dialect; PostgreSQL-specific features untested

---

## 4. PIN-Based Authentication

**Decision**: Custom middleware with session-based PIN validation (no ASP.NET Identity)

**Rationale**:
- FR-020/021 require PIN-only auth without user accounts
- ASP.NET Identity overkill for simple PIN validation
- Session cookie tracks judge identity within event scope

**Implementation Pattern**:
```csharp
// PIN validation service
public class PinAuthService
{
    public async Task<PinValidationResult> ValidatePin(Guid eventId, string pin, PinType type)
    {
        var battleEvent = await _context.Events.FindAsync(eventId);
        return type switch
        {
            PinType.Admin => battleEvent?.AdminPinHash == HashPin(pin) 
                ? PinValidationResult.Success(UserRole.Organizer) 
                : PinValidationResult.Failed(),
            PinType.Judge => battleEvent?.JudgePinHash == HashPin(pin)
                ? PinValidationResult.Success(UserRole.Judge)
                : PinValidationResult.Failed(),
            _ => PinValidationResult.Failed()
        };
    }
}

// Middleware or filter
public class PinAuthorizationFilter : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var eventId = context.RouteData.Values["eventId"] as string;
        var pin = context.HttpContext.Session.GetString("EventPin");
        // Validate and set claims
    }
}
```

**Key Considerations**:
- Store PIN hashes (not plaintext) in database
- Use `IDataProtector` or simple SHA256 for hashing
- Session cookie scoped to event; expires on browser close
- No email/password recovery - organizer creates new event if PIN forgotten

**Alternatives Considered**:
- ASP.NET Identity: Rejected - requires user accounts; violates simplicity for PIN-only use case
- JWT tokens: Rejected - adds complexity; session cookie sufficient for server-rendered app
- OAuth: Rejected - external dependency; spec explicitly chose PIN for simplicity

---

## 5. OpenAPI/Swagger Generation

**Decision**: Use Swashbuckle.AspNetCore with XML comments for API documentation

**Rationale**:
- Constitution IV requires OpenAPI contracts before implementation
- Swashbuckle is mature, widely adopted for ASP.NET Core
- XML comments provide inline documentation that generates OpenAPI descriptions

**Implementation Pattern**:
```csharp
// Program.cs
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "BreakingScoreBoard API", 
        Version = "v1" 
    });
    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
});

// Enable in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

**Project file addition**:
```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn> <!-- Suppress missing XML comment warnings -->
</PropertyGroup>
```

**Alternatives Considered**:
- NSwag: Viable alternative but Swashbuckle more common in tutorials/docs
- Manual OpenAPI YAML: Rejected - auto-generation reduces drift between code and docs
- Minimal APIs only: Considered - could use endpoint filters, but Controllers provide better Swagger integration

---

## Summary of Decisions

| Area | Decision | Key Package |
|------|----------|-------------|
| Database | EF Core 8 + Npgsql code-first | `Npgsql.EntityFrameworkCore.PostgreSQL` |
| Real-time | Blazor Server built-in SignalR | (included in framework) |
| Testing | Testcontainers for PostgreSQL | `Testcontainers.PostgreSql` |
| Authentication | Custom PIN middleware + sessions | (built-in `ISession`) |
| API Docs | Swashbuckle OpenAPI generation | `Swashbuckle.AspNetCore` |

## Deferred Decisions

- **Caching**: Not needed for MVP scale (32 breakers/category); add Redis if needed later
- **Background jobs**: Bracket advancement is synchronous trigger; no Hangfire/Quartz needed
- **Containerization**: Dockerfile deferred to deployment phase; local development uses `dotnet run`
