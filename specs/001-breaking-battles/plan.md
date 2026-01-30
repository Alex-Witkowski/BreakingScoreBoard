# Implementation Plan: Breaking Battle Management System

**Branch**: `001-breaking-battles` | **Date**: 2026-01-30 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-breaking-battles/spec.md`

## Summary

A web application for managing breaking (breakdance) battles with age categories, pre-selection rounds, knockout brackets (Top32→Final), and judge scoring. Core features: PIN-based authentication for organizers/judges, real-time score submission with reveal countdown, overflow-based pre-selection, bracket progression with walkover handling, and public spectator views.

**Technical Approach**: Server-rendered Blazor application with ASP.NET Core API backend, PostgreSQL persistence via EF Core, following Clean Architecture with domain-centric design. TDD enforced per constitution.

## Technical Context

**Language/Version**: C# / .NET 8 (LTS)  
**Primary Dependencies**: ASP.NET Core 8, Entity Framework Core 8, Blazor Server  
**Storage**: PostgreSQL 15+ with EF Core (Npgsql provider)  
**Testing**: xUnit, Testcontainers for PostgreSQL integration tests  
**Target Platform**: Linux server (containerized deployment)  
**Project Type**: Web application (server-rendered)  
**Performance Goals**: <1s standings query, <5s bracket advancement, <5s scoreboard update  
**Constraints**: Support 5 concurrent battles (15 judges scoring simultaneously) without data corruption  
**Scale/Scope**: 32 breakers per category, 5+ age categories per event, 3-5 judges per event

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence |
|-----------|--------|----------|
| **I. Test-First (NON-NEGOTIABLE)** | ✅ PASS | Tasks will follow Red→Green→Refactor; integration tests for scoring, brackets, judge calculations per spec SC-003, SC-006 |
| **II. Clean Architecture** | ✅ PASS | 3-project structure: Domain (entities, rules), API (controllers, Blazor), Tests; dependencies point inward |
| **III. Observability** | ✅ PASS | FR-011 requires audit logging for all scoring events; structured logging with correlation IDs planned |
| **IV. API-First Design** | ✅ PASS | OpenAPI contracts generated in Phase 1 before implementation; DTOs in contracts/ directory |
| **V. Simplicity** | ✅ PASS | 3 projects (max allowed); EF Core direct queries; no Repository/CQRS patterns |

**Pre-Design Gate**: ✅ PASSED — No violations identified. Proceed to Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/001-breaking-battles/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (OpenAPI specs)
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── BreakingScoreBoard.Domain/           # Domain layer (zero dependencies)
│   ├── Entities/                        # Breaker, Battle, Event, Score, etc.
│   ├── ValueObjects/                    # AgeCategory, BracketLevel, Pin, etc.
│   ├── Services/                        # Domain services (ScoringService, BracketService)
│   └── BreakingScoreBoard.Domain.csproj
│
├── BreakingScoreBoard.Api/              # Presentation + Infrastructure layer
│   ├── Controllers/                     # API endpoints (Events, Battles, Scores)
│   ├── Pages/                           # Blazor Server pages (Organizer, Judge, Spectator)
│   ├── Components/                      # Blazor components (ScoreInput, BracketView, Countdown)
│   ├── Infrastructure/                  # EF Core DbContext, Migrations, Repositories
│   ├── Contracts/                       # DTOs, request/response models
│   ├── Program.cs
│   └── BreakingScoreBoard.Api.csproj
│
└── BreakingScoreBoard.Tests/            # All tests
    ├── Unit/                            # Domain logic tests
    ├── Integration/                     # Database + API tests (Testcontainers)
    ├── Contract/                        # API contract tests
    └── BreakingScoreBoard.Tests.csproj

BreakingScoreBoard.sln                   # Solution file
```

**Structure Decision**: Single solution with 3 projects following Clean Architecture. Domain project has zero NuGet dependencies (pure C#). API project references Domain and contains both presentation (Blazor) and infrastructure (EF Core). Tests project references both for full coverage. This meets Constitution V. Simplicity (max 3 projects).

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

*No violations identified. All principles satisfied with standard approach.*

---

## Post-Design Constitution Check

*Re-evaluated after Phase 1 design artifacts generated.*

| Principle | Status | Design Evidence |
|-----------|--------|-----------------|
| **I. Test-First (NON-NEGOTIABLE)** | ✅ PASS | Data model entities (data-model.md) enable unit tests; OpenAPI contracts enable contract tests; Testcontainers researched for integration tests |
| **II. Clean Architecture** | ✅ PASS | Domain entities (Breaker, Battle, Score) have no infrastructure deps; DTOs separate from domain; 3-project structure maintains inward dependencies |
| **III. Observability** | ✅ PASS | Structured logging pattern defined in research.md; FR-011 audit logging endpoints in OpenAPI spec; correlation ID strategy documented |
| **IV. API-First Design** | ✅ PASS | Full OpenAPI 3.0.3 spec in contracts/openapi.yaml; all endpoints documented before implementation; DTOs defined as schemas |
| **V. Simplicity** | ✅ PASS | Single solution with 3 projects; no Repository pattern; EF Core direct queries; no CQRS/Event Sourcing |

**Post-Design Gate**: ✅ PASSED — Design artifacts comply with all Constitution principles. Ready for `/speckit.tasks`.
