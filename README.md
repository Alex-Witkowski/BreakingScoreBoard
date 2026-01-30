# Breaking Battle ScoreBoard

> **A comprehensive breaking (breakdance) battle management system with knockout brackets, judge scoring, and pre-selection rounds.**

[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15%2B-336791)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

---

## Features

- **📅 Event Management**: Create and manage breaking battle events with multiple age categories
- **👥 Breaker Registration**: Register participants with age-based categories (U14, U16, U18, Open)
- **🏆 Tournament Brackets**: Automatic knockout bracket generation with pre-selection rounds
- **⚖️ Judge Scoring**: Multi-judge scoring (3 or 5 judges) with average calculation and forced differentiation
- **📊 Live Scoreboard**: Real-time battle results and standings for spectators
- **🔐 PIN Authentication**: Secure admin and judge access without complex user accounts
- **🎯 Battle Flow**: In-progress tracking, reveal countdowns, walkover support
- **📱 Blazor UI**: Interactive web interface for organizers, judges, and spectators

---

## Quick Start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL 15+](https://www.postgresql.org/download/) (or use existing dev container)
- IDE: Visual Studio 2022, VS Code with C# Dev Kit, or JetBrains Rider

### Running the Application

```bash
# Clone the repository
git clone <repository-url>
cd dotnet-postgres

# Restore dependencies
dotnet restore

# Run database migrations
cd src/BreakingScoreBoard.Api
dotnet ef database update

# Start the application
dotnet run
```

The API will be available at:
- **API**: `http://localhost:8080`
- **Swagger UI**: `http://localhost:8080/swagger`
- **Blazor Pages**:
  - Organizer Dashboard: `http://localhost:8080/organizer/dashboard`
  - Judge Scoring: `http://localhost:8080/judge/scoring`
  - Spectator Scoreboard: `http://localhost:8080/spectator/scoreboard`

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test category
dotnet test --filter "FullyQualifiedName~Integration"
```

---

## API Documentation

### Core Endpoints

#### Events

- `POST /events` - Create a new battle event
- `GET /events/{id}` - Get event details
- `GET /events` - List all events
- `PUT /events/{id}` - Update event (requires admin PIN)
- `DELETE /events/{id}` - Delete event (requires admin PIN)

#### Categories

- `POST /events/{eventId}/categories/{categoryId}/start-preselection` - Start pre-selection round
- `POST /events/{eventId}/categories/{categoryId}/start-bracket` - Start knockout bracket
- `POST /events/{eventId}/categories/{categoryId}/advance` - Advance to next bracket round

#### Registrations

- `POST /registrations` - Register a breaker for a category
- `GET /events/{eventId}/categories/{categoryId}/registrations` - List registrations

#### Battles

- `GET /battles/{id}` - Get battle details
- `POST /battles/{id}/walkover` - Record walkover (winner by default)

#### Scores

- `POST /scores` - Submit judge scores (requires judge PIN)
- `GET /battles/{id}/scores` - Get all scores for a battle

#### Public (No Authentication Required)

- `GET /public/events/{eventId}/scoreboard` - Get live scoreboard
- `GET /public/events/{eventId}/standings` - Get category standings
- `GET /public/battles/{battleId}/live` - Get live battle details

### Authentication

The API uses PIN-based authentication via the `X-Pin` header:

```bash
# Admin operations
curl -H "X-Pin: your-admin-pin" -X DELETE http://localhost:8080/events/{id}

# Judge score submission
curl -H "X-Pin: your-judge-pin" -X POST http://localhost:8080/scores \
  -H "Content-Type: application/json" \
  -d '{"battleId":"...","judgeIdentifier":"J1","breaker1Score":85,"breaker2Score":90}'
```

### Response Format

All API responses follow a consistent format:

**Success (200/201)**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "title": "Breaking Championship 2026",
  "eventDate": "2026-06-15",
  ...
}
```

**Error (400/404/500)**:
```json
{
  "message": "Event not found",
  "timestamp": "2026-01-30T12:34:56.789Z"
}
```

---

## Architecture

### Project Structure

```
BreakingScoreBoard/
├── src/
│   ├── BreakingScoreBoard.Domain/     # Core business logic
│   │   ├── Entities/                   # Domain models
│   │   ├── Enums/                      # Enumerations
│   │   └── Services/                   # Domain services
│   │
│   ├── BreakingScoreBoard.Api/        # Web API + Blazor UI
│   │   ├── Controllers/                # REST API controllers
│   │   ├── Contracts/                  # DTOs (requests/responses)
│   │   ├── Infrastructure/             # DbContext, middleware, auth
│   │   ├── Pages/                      # Blazor pages
│   │   ├── Components/                 # Blazor components
│   │   └── Program.cs                  # Application startup
│   │
│   └── BreakingScoreBoard.Tests/      # Test suite
│       ├── Unit/                       # Domain logic tests
│       └── Integration/                # API integration tests
│
└── specs/                              # Feature specifications
    └── 001-breaking-battles/           # Current feature docs
```

### Technology Stack

- **Framework**: ASP.NET Core 9.0 (Web API + Blazor Server)
- **ORM**: Entity Framework Core 9.0
- **Database**: PostgreSQL 15+
- **Testing**: xUnit + FluentAssertions + WebApplicationFactory
- **API Docs**: Swagger/OpenAPI 3.0

### Domain Model

```
BattleEvent
├── AgeCategories[]
│   ├── Battles[]
│   │   ├── Breaker1
│   │   ├── Breaker2
│   │   └── JudgeScores[]
│   └── Registrations[]
│       └── Breaker
```

---

## Configuration

### Database Connection

Update `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=breakingscoreboard;Username=postgres;Password=postgres"
  }
}
```

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | See appsettings.json |
| `ASPNETCORE_ENVIRONMENT` | Environment (Development/Production) | Development |
| `ASPNETCORE_URLS` | Listen URLs | `http://+:8080` |

---

## Development

### Code Formatting

```bash
# Apply formatting
dotnet format

# Verify formatting
dotnet format --verify-no-changes
```

### Database Migrations

```bash
cd src/BreakingScoreBoard.Api

# Create new migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Rollback migration
dotnet ef database update PreviousMigrationName
```

### Adding Features

1. Define specification in `specs/{feature-id}/`
2. Run `/speckit.tasks` to generate task list
3. Follow TDD: Write test → Implement → Refactor
4. Update documentation

---

## Deployment

### Docker (Recommended)

```bash
# Build image
docker build -t breakingscoreboard-api .

# Run with PostgreSQL
docker run -d \
  -e ConnectionStrings__DefaultConnection="Host=db;..." \
  -p 8080:8080 \
  breakingscoreboard-api
```

### Azure App Service

1. Create PostgreSQL Flexible Server
2. Set connection string in App Settings
3. Deploy via GitHub Actions or Azure CLI

---

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Write tests for your changes
4. Ensure all tests pass (`dotnet test`)
5. Commit your changes (`git commit -m 'Add amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## Support

- **Documentation**: See `specs/001-breaking-battles/` for detailed specifications
- **Quickstart**: See `specs/001-breaking-battles/quickstart.md` for setup guide
- **API Reference**: Navigate to `/swagger` when running the application

---

## Acknowledgments

- Built for the breaking (breakdance) community
- Designed for competitive battle management
- Inspired by real-world tournament needs
