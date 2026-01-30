# Quickstart: Breaking Battle Management System

**Feature**: 001-breaking-battles  
**Date**: 2026-01-30

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (LTS)
- [PostgreSQL 15+](https://www.postgresql.org/download/) or Docker
- IDE: Visual Studio 2022, VS Code with C# Dev Kit, or JetBrains Rider

## Quick Setup (5 minutes)

### 1. Clone and Navigate

```bash
cd /workspaces/dotnet-postgres
git checkout 001-breaking-battles
```

### 2. Start PostgreSQL (Docker)

```bash
docker run -d \
  --name breakingscoreboard-db \
  -e POSTGRES_USER=breakingscoreboard \
  -e POSTGRES_PASSWORD=devpassword \
  -e POSTGRES_DB=breakingscoreboard \
  -p 5432:5432 \
  postgres:15-alpine
```

### 3. Create Solution Structure

```bash
# Create solution
dotnet new sln -n BreakingScoreBoard

# Create projects
dotnet new classlib -n BreakingScoreBoard.Domain -o src/BreakingScoreBoard.Domain
dotnet new web -n BreakingScoreBoard.Api -o src/BreakingScoreBoard.Api
dotnet new xunit -n BreakingScoreBoard.Tests -o src/BreakingScoreBoard.Tests

# Add to solution
dotnet sln add src/BreakingScoreBoard.Domain
dotnet sln add src/BreakingScoreBoard.Api
dotnet sln add src/BreakingScoreBoard.Tests

# Add project references
dotnet add src/BreakingScoreBoard.Api reference src/BreakingScoreBoard.Domain
dotnet add src/BreakingScoreBoard.Tests reference src/BreakingScoreBoard.Domain
dotnet add src/BreakingScoreBoard.Tests reference src/BreakingScoreBoard.Api
```

### 4. Install NuGet Packages

```bash
# API project - EF Core + PostgreSQL + Swagger
cd src/BreakingScoreBoard.Api
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.*
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.*
dotnet add package Swashbuckle.AspNetCore --version 6.*

# Tests project - Testcontainers + WebApplicationFactory
cd ../BreakingScoreBoard.Tests
dotnet add package Testcontainers.PostgreSql --version 3.*
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 8.*
dotnet add package FluentAssertions --version 6.*

cd ../..
```

### 5. Configure Connection String

Create or update `src/BreakingScoreBoard.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=breakingscoreboard;Username=breakingscoreboard;Password=devpassword"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

### 6. Run the Application

```bash
cd src/BreakingScoreBoard.Api
dotnet run
```

Application starts at:
- **Blazor UI**: https://localhost:5001
- **Swagger UI**: https://localhost:5001/swagger

### 7. Run Tests

```bash
# From solution root
dotnet test

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

---

## Development Workflow

### TDD Cycle (Constitution I)

```bash
# 1. Write failing test
dotnet test --filter "FullyQualifiedName~MyNewTest"
# ❌ Test fails (Red)

# 2. Implement minimal code
# ... edit source files ...

# 3. Run test again
dotnet test --filter "FullyQualifiedName~MyNewTest"
# ✅ Test passes (Green)

# 4. Refactor
dotnet format
dotnet test  # Ensure still green
```

### Database Migrations

```bash
cd src/BreakingScoreBoard.Api

# Create migration
dotnet ef migrations add InitialCreate

# Apply migration
dotnet ef database update

# Revert migration
dotnet ef database update PreviousMigrationName
```

### Code Quality

```bash
# Format code
dotnet format

# Build with warnings as errors
dotnet build /warnaserror

# Verify no formatting changes needed
dotnet format --verify-no-changes
```

---

## Project Structure Reference

```
BreakingScoreBoard/
├── BreakingScoreBoard.sln
├── src/
│   ├── BreakingScoreBoard.Domain/
│   │   ├── Entities/           # Breaker, Battle, Event, Score
│   │   ├── ValueObjects/       # AgeCategory, BracketLevel, Pin
│   │   ├── Services/           # ScoringService, BracketService
│   │   └── BreakingScoreBoard.Domain.csproj
│   │
│   ├── BreakingScoreBoard.Api/
│   │   ├── Controllers/        # EventsController, BattlesController
│   │   ├── Pages/              # Blazor pages
│   │   ├── Components/         # Blazor components
│   │   ├── Infrastructure/     # DbContext, Migrations
│   │   ├── Contracts/          # DTOs
│   │   ├── Program.cs
│   │   └── BreakingScoreBoard.Api.csproj
│   │
│   └── BreakingScoreBoard.Tests/
│       ├── Unit/               # Domain logic tests
│       ├── Integration/        # Database tests (Testcontainers)
│       ├── Contract/           # API contract tests
│       └── BreakingScoreBoard.Tests.csproj
│
└── specs/
    └── 001-breaking-battles/   # This feature's documentation
```

---

## Common Tasks

### Create a Test Event (via Swagger)

1. Open https://localhost:5001/swagger
2. POST `/api/v1/events` with:
   ```json
   {
     "title": "Test Battle 2026",
     "eventDate": "2026-03-15",
     "adminPin": "1234",
     "judgeCount": 3,
     "categories": [
       { "name": "U14", "maxAge": 14, "bracketSize": 16 },
       { "name": "Open", "maxAge": null, "bracketSize": 32 }
     ]
   }
   ```
3. Save the returned `id` and `judgePin`

### Access Judge Interface

1. Navigate to `/judge/{eventId}`
2. Enter judge PIN when prompted
3. Select active battle to score

### View Public Scoreboard

1. Navigate to `/scoreboard/{eventId}`
2. No authentication required
3. Scores update in real-time via SignalR

---

## Troubleshooting

### PostgreSQL Connection Failed

```bash
# Check if container is running
docker ps | grep breakingscoreboard-db

# View logs
docker logs breakingscoreboard-db

# Restart if needed
docker restart breakingscoreboard-db
```

### EF Core Migration Errors

```bash
# Reset database (development only!)
docker exec breakingscoreboard-db psql -U breakingscoreboard -c "DROP SCHEMA public CASCADE; CREATE SCHEMA public;"
dotnet ef database update
```

### Testcontainers: Docker Not Available

Ensure Docker daemon is running. On Linux:
```bash
sudo systemctl start docker
```

On macOS/Windows: Start Docker Desktop.

---

## Environment Variables (Production)

| Variable | Description | Example |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `Host=db;...` |
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Production` |
| `ASPNETCORE_URLS` | Listen URLs | `http://+:8080` |

---

## Next Steps

After setup:

1. Run `/speckit.tasks` to generate implementation task list
2. Start with Phase 1 (Setup) tasks
3. Follow TDD for each feature per Constitution
