<!--
  SYNC IMPACT REPORT
  ==================
  Version Change: N/A → 1.0.0 (initial)
  
  Added Principles:
  - I. Test-First Development (NON-NEGOTIABLE)
  - II. Clean Architecture
  - III. Observability
  - IV. API-First Design
  - V. Simplicity
  
  Added Sections:
  - Technology Standards
  - Development Workflow
  - Governance
  
  Removed Sections: None (initial version)
  
  Templates Validated:
  - ✅ plan-template.md - Constitution Check section present
  - ✅ spec-template.md - Requirements and acceptance criteria aligned
  - ✅ tasks-template.md - Phase structure supports TDD workflow
  
  Deferred Items: None
-->

# BreakingScoreBoard Constitution

A web application for managing breaking (breakdance) battles with age categories,
pre-selection rounds, knockout brackets (Top32, Top16, etc.), and judge scoring.

## Core Principles

### I. Test-First Development (NON-NEGOTIABLE)

All feature implementation MUST follow Test-Driven Development:

- Tests MUST be written before implementation code
- Tests MUST fail before implementation begins (Red phase)
- Implementation MUST make tests pass with minimal code (Green phase)
- Code MUST be refactored after tests pass (Refactor phase)
- Integration tests MUST cover: battle scoring, bracket progression, judge calculations

**Rationale**: A competition scoring system demands correctness. Incorrect scores
or bracket progressions would undermine trust. TDD ensures logic is verified before
deployment.

### II. Clean Architecture

The codebase MUST follow domain-centric layered architecture:

- **Domain Layer**: Core entities (Breaker, Battle, Round, Judge, Score) with business rules
- **Application Layer**: Use cases and service interfaces (no infrastructure concerns)
- **Infrastructure Layer**: Database (PostgreSQL/EF Core), external services
- **Presentation Layer**: ASP.NET API controllers, Blazor/Razor pages

Dependencies MUST point inward (Infrastructure → Application → Domain).
Domain layer MUST have zero external dependencies.

**Rationale**: Separation enables independent testing of scoring logic, bracket
algorithms, and UI without database coupling. Supports future platform changes.

### III. Observability

All operations MUST be traceable and measurable:

- Structured logging MUST use Microsoft.Extensions.Logging with correlation IDs
- Battle events (score submission, bracket advancement) MUST be logged with context
- Performance metrics MUST track: API response times, database query duration
- Errors MUST include stack traces, request context, and user identifiers (anonymized)

**Rationale**: Live competition events require real-time visibility into system health.
Judges and organizers need confidence the system is recording accurately.

### IV. API-First Design

All features MUST define contracts before implementation:

- API endpoints MUST be documented in OpenAPI/Swagger format
- Request/response DTOs MUST be defined in contracts/ before services
- Breaking changes MUST follow semantic versioning with deprecation notices
- Client SDKs (if any) MUST be generated from OpenAPI specs

**Rationale**: Frontend developers and potential mobile apps need stable contracts.
Competition organizers may integrate with external display systems.

### V. Simplicity

Solutions MUST favor the simplest approach that meets requirements:

- YAGNI: Do NOT implement features until explicitly needed
- Maximum 3 projects in solution unless justified (e.g., API, Domain, Tests)
- Avoid patterns (Repository, CQRS) unless complexity warrants them
- Database queries SHOULD use EF Core directly until performance requires optimization

**Rationale**: Competition management is straightforward CRUD with scoring logic.
Over-engineering obscures the domain and slows delivery.

## Technology Standards

The project MUST use the following stack:

- **Runtime**: .NET 8+ (LTS)
- **Database**: PostgreSQL 15+ with Entity Framework Core
- **Testing**: xUnit for unit/integration tests, Testcontainers for database tests
- **API**: ASP.NET Core Minimal APIs or Controllers with OpenAPI
- **Frontend**: Blazor Server or Razor Pages (server-rendered preferred for simplicity)

Code style MUST follow:

- EditorConfig and .NET analyzers enforced
- Nullable reference types enabled project-wide
- `dotnet format` MUST pass before commits

## Development Workflow

### Code Review Requirements

- All changes MUST be submitted via pull request
- PRs MUST include: description, test coverage, Constitution compliance statement
- At least one approval required before merge

### Quality Gates

Before merge, all PRs MUST pass:

1. `dotnet build` - Zero warnings (treat warnings as errors)
2. `dotnet test` - All tests pass
3. `dotnet format --verify-no-changes` - Code formatted
4. Constitution Check - Principles verified (documented in PR)

### Feature Development Flow

1. Spec created (`/speckit.specify`)
2. Plan created (`/speckit.plan`) with Constitution Check
3. Tasks generated (`/speckit.tasks`)
4. TDD implementation (Red → Green → Refactor)
5. PR with compliance verification
6. Merge and deploy

## Governance

This Constitution supersedes all other development practices for BreakingScoreBoard.

### Amendment Process

1. Propose change with rationale (issue or PR)
2. Document impact on existing code and principles
3. Update dependent templates if principles change
4. Increment version per semantic versioning:
   - MAJOR: Principle removed or fundamentally redefined
   - MINOR: New principle or section added
   - PATCH: Clarification or wording improvement
5. Update `Last Amended` date

### Compliance

- All PRs MUST include Constitution compliance verification
- Violations MUST be documented with justification in Complexity Tracking
- Unjustified violations block merge

**Version**: 1.0.0 | **Ratified**: 2026-01-30 | **Last Amended**: 2026-01-30
