# Tasks: Breaking Battle Management System

**Input**: Design documents from `/specs/001-breaking-battles/`
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/openapi.yaml ✓

**Tests**: TDD is MANDATORY per Constitution Principle I. Tests are written before implementation.

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: User story mapping (US1-US6)
- File paths follow plan.md structure

---

## Phase 1: Setup (Project Initialization)

**Purpose**: Create solution structure and configure development environment

- [X] T001 Create solution and project structure per quickstart.md
- [X] T002 [P] Add NuGet packages to BreakingScoreBoard.Api (Npgsql.EntityFrameworkCore.PostgreSQL, Swashbuckle.AspNetCore)
- [X] T003 [P] Add NuGet packages to BreakingScoreBoard.Tests (Testcontainers.PostgreSql, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing)
- [X] T004 [P] Configure appsettings.Development.json with PostgreSQL connection string
- [X] T005 [P] Enable XML documentation in Api.csproj for Swagger

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST complete before any user story

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Domain Entities

- [X] T006 [P] Create CategoryPhase enum in src/BreakingScoreBoard.Domain/Enums/CategoryPhase.cs
- [X] T007 [P] Create BattleStatus enum in src/BreakingScoreBoard.Domain/Enums/BattleStatus.cs
- [X] T008 [P] Create BracketLevel enum in src/BreakingScoreBoard.Domain/Enums/BracketLevel.cs
- [X] T009 [P] Create RegistrationStatus enum in src/BreakingScoreBoard.Domain/Enums/RegistrationStatus.cs
- [X] T010 [P] Create BattleEvent entity in src/BreakingScoreBoard.Domain/Entities/BattleEvent.cs
- [X] T011 [P] Create AgeCategory entity in src/BreakingScoreBoard.Domain/Entities/AgeCategory.cs
- [X] T012 [P] Create Breaker entity in src/BreakingScoreBoard.Domain/Entities/Breaker.cs
- [X] T013 [P] Create Registration entity in src/BreakingScoreBoard.Domain/Entities/Registration.cs
- [X] T014 [P] Create Battle entity in src/BreakingScoreBoard.Domain/Entities/Battle.cs
- [X] T015 [P] Create JudgeScore entity in src/BreakingScoreBoard.Domain/Entities/JudgeScore.cs

### Infrastructure

- [X] T016 Create BattleDbContext with DbSets in src/BreakingScoreBoard.Api/Infrastructure/BattleDbContext.cs
- [X] T017 Configure entity relationships and indexes in BattleDbContext.OnModelCreating
- [X] T018 Create initial EF Core migration
- [X] T019 [P] Create PIN hashing utility in src/BreakingScoreBoard.Api/Infrastructure/PinHasher.cs
- [X] T020 [P] Create PinAuthService in src/BreakingScoreBoard.Api/Infrastructure/PinAuthService.cs
- [X] T021 Create PinAuthorizationFilter in src/BreakingScoreBoard.Api/Infrastructure/PinAuthorizationFilter.cs
- [X] T022 [P] Create ErrorResponse DTO in src/BreakingScoreBoard.Api/Contracts/ErrorResponse.cs
- [X] T023 Configure Swagger and exception handling middleware in Program.cs
- [X] T024 Create DatabaseFixture for Testcontainers in src/BreakingScoreBoard.Tests/Integration/DatabaseFixture.cs

**Checkpoint**: Foundation ready - user story implementation can begin

---

## Phase 3: User Story 1 - Organizer Creates Battle Event (Priority: P1) 🎯 MVP

**Goal**: Organizers can create events with age categories and PIN authentication

**Independent Test**: Create event → Query by ID → Verify categories stored correctly

### Tests for User Story 1

- [X] T025 [P] [US1] Unit test BattleEvent validation (judge count 3/5, title required) in src/BreakingScoreBoard.Tests/Unit/Entities/BattleEventTests.cs
- [X] T026 [P] [US1] Unit test AgeCategory validation (maxAge, bracketSize power of 2) in src/BreakingScoreBoard.Tests/Unit/Entities/AgeCategoryTests.cs
- [X] T027 [P] [US1] Integration test POST /events creates event with categories in src/BreakingScoreBoard.Tests/Integration/EventsControllerTests.cs
- [X] T028 [P] [US1] Integration test GET /events/{id} returns event with categories in src/BreakingScoreBoard.Tests/Integration/EventsControllerTests.cs
- [X] T029 [P] [US1] Integration test PATCH /events/{id} blocked after battle starts in src/BreakingScoreBoard.Tests/Integration/EventsControllerTests.cs

### Implementation for User Story 1

- [X] T030 [P] [US1] Create CreateEventRequest DTO in src/BreakingScoreBoard.Api/Contracts/Events/CreateEventRequest.cs
- [X] T031 [P] [US1] Create UpdateEventRequest DTO in src/BreakingScoreBoard.Api/Contracts/Events/UpdateEventRequest.cs
- [X] T032 [P] [US1] Create EventResponse DTO in src/BreakingScoreBoard.Api/Contracts/Events/EventResponse.cs
- [X] T033 [P] [US1] Create CreateCategoryRequest DTO in src/BreakingScoreBoard.Api/Contracts/Categories/CreateCategoryRequest.cs
- [X] T034 [P] [US1] Create CategoryResponse DTO in src/BreakingScoreBoard.Api/Contracts/Categories/CategoryResponse.cs
- [X] T035 [US1] Implement EventsController with POST, GET, PATCH endpoints in src/BreakingScoreBoard.Api/Controllers/EventsController.cs
- [X] T036 [US1] Implement POST /events/{eventId}/regenerate-judge-pin in EventsController
- [X] T037 [US1] Implement CategoriesController with GET, POST endpoints in src/BreakingScoreBoard.Api/Controllers/CategoriesController.cs
- [X] T038 [US1] Add validation: prevent category/judge count changes after first battle (FR-013)

**Checkpoint**: User Story 1 complete - Events can be created and queried

---

## Phase 4: User Story 2 - Judge Scores Breakers (Priority: P1) 🎯 MVP

**Goal**: Judges submit independent scores for each breaker; winner calculated by average

**Independent Test**: Create battle → Submit scores from 3 judges → Verify winner by highest average

### Tests for User Story 2

- [X] T039 [P] [US2] Unit test score calculation (average of judge scores) in src/BreakingScoreBoard.Tests/Unit/Services/ScoringServiceTests.cs
- [X] T040 [P] [US2] Unit test tie detection triggers re-battle in src/BreakingScoreBoard.Tests/Unit/Services/ScoringServiceTests.cs
- [X] T041 [P] [US2] Unit test forced differentiation in re-battles in src/BreakingScoreBoard.Tests/Unit/Services/ScoringServiceTests.cs
- [X] T042 [P] [US2] Integration test POST /scores submits judge scores in src/BreakingScoreBoard.Tests/Integration/ScoresControllerTests.cs
- [X] T043 [P] [US2] Integration test score resubmission before reveal allowed in src/BreakingScoreBoard.Tests/Integration/ScoresControllerTests.cs
- [X] T044 [P] [US2] Integration test score locked after reveal countdown in src/BreakingScoreBoard.Tests/Integration/ScoresControllerTests.cs
- [X] T045 [P] [US2] Integration test score validation (0-100 range) in src/BreakingScoreBoard.Tests/Integration/ScoresControllerTests.cs

### Implementation for User Story 2

- [X] T046 [P] [US2] Create ScoringService in src/BreakingScoreBoard.Domain/Services/ScoringService.cs
- [X] T047 [P] [US2] Create SubmitScoresRequest DTO in src/BreakingScoreBoard.Api/Contracts/Scores/SubmitScoresRequest.cs
- [X] T048 [P] [US2] Create BattleResponse DTO in src/BreakingScoreBoard.Api/Contracts/Battles/BattleResponse.cs
- [X] T049 [P] [US2] Create BattleDetailResponse DTO in src/BreakingScoreBoard.Api/Contracts/Battles/BattleDetailResponse.cs
- [X] T050 [P] [US2] Create JudgeScoreResponse DTO in src/BreakingScoreBoard.Api/Contracts/Scores/JudgeScoreResponse.cs
- [X] T051 [US2] Implement BattlesController with GET /battles, GET /battles/{id} in src/BreakingScoreBoard.Api/Controllers/BattlesController.cs
- [X] T052 [US2] Implement POST /battles/{id}/start in BattlesController
- [X] T053 [US2] Implement POST /battles/{id}/reveal with countdown logic in BattlesController
- [X] T054 [US2] Implement ScoresController with POST /scores in src/BreakingScoreBoard.Api/Controllers/ScoresController.cs
- [X] T055 [US2] Implement winner calculation and tie detection in ScoringService
- [X] T056 [US2] Implement automatic re-battle creation on tie in ScoringService
- [X] T057 [US2] Add score locking after reveal countdown (FR-024)
- [X] T058 [US2] Add audit logging for all score submissions (FR-011)

**Checkpoint**: User Story 2 complete - Judges can score battles and winners are determined

---

## Phase 5: User Story 5 - Breaker Registration (Priority: P2)

**Goal**: Breakers register for events in age-appropriate categories

**Independent Test**: Register breaker → Verify appears in category participant list

### Tests for User Story 5

- [X] T059 [P] [US5] Unit test age calculation from birth date in src/BreakingScoreBoard.Tests/Unit/Entities/BreakerTests.cs
- [X] T060 [P] [US5] Unit test age category validation (age fits maxAge) in src/BreakingScoreBoard.Tests/Unit/Entities/RegistrationTests.cs
- [X] T061 [P] [US5] Integration test POST /registrations creates registration in src/BreakingScoreBoard.Tests/Integration/RegistrationsControllerTests.cs
- [X] T062 [P] [US5] Integration test registration rejected if age doesn't fit category in src/BreakingScoreBoard.Tests/Integration/RegistrationsControllerTests.cs
- [X] T063 [P] [US5] Integration test duplicate registration replaces existing in src/BreakingScoreBoard.Tests/Integration/RegistrationsControllerTests.cs

### Implementation for User Story 5

- [X] T064 [P] [US5] Create RegisterBreakerRequest DTO in src/BreakingScoreBoard.Api/Contracts/Registrations/RegisterBreakerRequest.cs
- [X] T065 [P] [US5] Create RegistrationResponse DTO in src/BreakingScoreBoard.Api/Contracts/Registrations/RegistrationResponse.cs
- [X] T066 [P] [US5] Create CategoryDetailResponse DTO in src/BreakingScoreBoard.Api/Contracts/Categories/CategoryDetailResponse.cs
- [X] T067 [US5] Implement RegistrationsController with POST endpoint in src/BreakingScoreBoard.Api/Controllers/RegistrationsController.cs
- [X] T068 [US5] Add age validation logic (breaker age must fit category maxAge) (FR-002)
- [X] T069 [US5] Implement duplicate registration handling (idempotent replace) (FR-012)
- [X] T070 [US5] Update CategoriesController GET to include registrations

**Checkpoint**: User Story 5 complete - Breakers can register for categories

---

## Phase 6: User Story 3 - Pre-Selection (Priority: P2)

**Goal**: System runs overflow-based pre-selection when registrations exceed bracket size

**Independent Test**: Register 36 breakers for 32-slot → Trigger pre-selection → Verify 4 battles created

### Tests for User Story 3

- [X] T071 [P] [US3] Unit test overflow calculation (registered - bracket size) in src/BreakingScoreBoard.Tests/Unit/Services/PreSelectionServiceTests.cs
- [X] T072 [P] [US3] Unit test random selection of 2×overflow breakers in src/BreakingScoreBoard.Tests/Unit/Services/PreSelectionServiceTests.cs
- [X] T073 [P] [US3] Integration test POST /start-preselection creates battles in src/BreakingScoreBoard.Tests/Integration/PreSelectionTests.cs
- [X] T074 [P] [US3] Integration test no pre-selection when registrations <= bracket size in src/BreakingScoreBoard.Tests/Integration/PreSelectionTests.cs
- [X] T075 [P] [US3] Integration test registration blocked after pre-selection starts (FR-019) in src/BreakingScoreBoard.Tests/Integration/PreSelectionTests.cs

### Implementation for User Story 3

- [X] T076 [US3] Create PreSelectionService in src/BreakingScoreBoard.Domain/Services/PreSelectionService.cs
- [X] T077 [US3] Implement overflow calculation in PreSelectionService (FR-016)
- [X] T078 [US3] Implement random selection of 2×overflow breakers in PreSelectionService (FR-017)
- [X] T079 [US3] Implement pre-selection battle creation in PreSelectionService (FR-018)
- [X] T080 [US3] Implement POST /categories/{id}/start-preselection in CategoriesController
- [X] T081 [US3] Block registrations when category phase changes to PreSelection (FR-019)

**Checkpoint**: User Story 3 complete - Pre-selection runs for overflow registrations

---

## Phase 7: User Story 4 - Bracket Progression (Priority: P2)

**Goal**: Winners automatically advance through knockout bracket levels

**Independent Test**: Complete all Top32 battles → Trigger advancement → Verify 16 winners in Top16

### Tests for User Story 4

- [X] T082 [P] [US4] Unit test bracket advancement logic in src/BreakingScoreBoard.Tests/Unit/Services/BracketServiceTests.cs
- [X] T083 [P] [US4] Unit test bye assignment for odd winners in src/BreakingScoreBoard.Tests/Unit/Services/BracketServiceTests.cs
- [X] T084 [P] [US4] Unit test idempotent advancement (FR-014) in src/BreakingScoreBoard.Tests/Unit/Services/BracketServiceTests.cs
- [X] T085 [P] [US4] Integration test POST /advance-bracket creates next level battles in src/BreakingScoreBoard.Tests/Integration/BracketProgressionTests.cs
- [X] T086 [P] [US4] Integration test walkover handling (FR-027, FR-028) in src/BreakingScoreBoard.Tests/Integration/BracketProgressionTests.cs

### Implementation for User Story 4

- [X] T087 [US4] Create BracketService in src/BreakingScoreBoard.Domain/Services/BracketService.cs
- [X] T088 [US4] Implement winner advancement to next bracket level in BracketService (FR-008)
- [X] T089 [US4] Implement bye selection for odd number of winners in BracketService (FR-009)
- [X] T090 [US4] Implement idempotent advancement in BracketService (FR-014)
- [X] T091 [US4] Implement POST /categories/{id}/advance-bracket in CategoriesController
- [X] T092 [US4] Implement POST /battles/{id}/walkover in BattlesController (FR-027)
- [X] T093 [US4] Record walkover status without numeric scores (FR-028)

**Checkpoint**: User Story 4 complete - Brackets progress through knockout rounds

---

## Phase 8: User Story 6 - Spectator View (Priority: P3)

**Goal**: Public real-time scoreboard and standings without authentication

**Independent Test**: Query public endpoints → Verify current bracket state returned anonymously

### Tests for User Story 6

- [X] T094 [P] [US6] Integration test GET /public/scoreboard returns active battles in src/BreakingScoreBoard.Tests/Integration/PublicControllerTests.cs
- [X] T095 [P] [US6] Integration test GET /public/standings returns bracket state in src/BreakingScoreBoard.Tests/Integration/PublicControllerTests.cs
- [X] T096 [P] [US6] Integration test GET /public/battles/{id}/live returns countdown in src/BreakingScoreBoard.Tests/Integration/PublicControllerTests.cs
- [X] T097 [P] [US6] Integration test public endpoints require no authentication in src/BreakingScoreBoard.Tests/Integration/PublicControllerTests.cs

### Implementation for User Story 6

- [X] T098 [P] [US6] Create ScoreboardResponse DTO in src/BreakingScoreBoard.Api/Contracts/Public/ScoreboardResponse.cs
- [X] T099 [P] [US6] Create StandingsResponse DTO in src/BreakingScoreBoard.Api/Contracts/Public/StandingsResponse.cs
- [X] T100 [P] [US6] Create LiveBattleResponse DTO in src/BreakingScoreBoard.Api/Contracts/Public/LiveBattleResponse.cs
- [X] T101 [P] [US6] Create BattleResultResponse DTO in src/BreakingScoreBoard.Api/Contracts/Public/BattleResultResponse.cs
- [X] T102 [P] [US6] Create CategoryStandings DTO in src/BreakingScoreBoard.Api/Contracts/Public/CategoryStandings.cs
- [X] T103 [P] [US6] Create BreakerStanding DTO in src/BreakingScoreBoard.Api/Contracts/Public/BreakerStanding.cs
- [X] T104 [US6] Implement PublicController with scoreboard, standings, live endpoints in src/BreakingScoreBoard.Api/Controllers/PublicController.cs
- [X] T105 [US6] Ensure public endpoints skip PIN authentication

**Checkpoint**: User Story 6 complete - Spectators can view live scores and standings

---

## Phase 9: Blazor UI (Optional Enhancement)

**Purpose**: Server-rendered Blazor pages for organizer, judge, and spectator interfaces

- [ ] T106 [P] Create OrganizerDashboard page in src/BreakingScoreBoard.Api/Pages/Organizer/Dashboard.razor
- [ ] T107 [P] Create JudgeScoringPage in src/BreakingScoreBoard.Api/Pages/Judge/Scoring.razor
- [ ] T108 [P] Create SpectatorScoreboard page in src/BreakingScoreBoard.Api/Pages/Spectator/Scoreboard.razor
- [ ] T109 Create ScoreInput component in src/BreakingScoreBoard.Api/Components/ScoreInput.razor
- [ ] T110 Create BracketView component in src/BreakingScoreBoard.Api/Components/BracketView.razor
- [ ] T111 Create CountdownTimer component with SignalR in src/BreakingScoreBoard.Api/Components/CountdownTimer.razor

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Final improvements across all user stories

- [ ] T112 [P] Add XML documentation comments to all public API endpoints
- [ ] T113 [P] Add structured logging with correlation IDs throughout application
- [ ] T114 Run dotnet format and verify code style
- [ ] T115 Verify all tests pass with Testcontainers
- [ ] T116 Run quickstart.md validation end-to-end
- [ ] T117 Update README.md with API documentation links

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1: Setup ─────────────────────────► (no dependencies)
                │
                ▼
Phase 2: Foundational ──────────────────► BLOCKS all user stories
                │
                ├──────────────────────────────────────────┐
                ▼                                          ▼
Phase 3: US1 (Event Creation) ──► Phase 4: US2 (Scoring) ──► (can run in parallel after Phase 2)
                │                          │
                ▼                          ▼
Phase 5: US5 (Registration) ────────────────────────────────► (needs events from US1)
                │
                ├───────────────────────┐
                ▼                       ▼
Phase 6: US3 (Pre-Selection)    Phase 7: US4 (Brackets) ────► (needs registrations)
                │                       │
                └───────────────────────┘
                                        ▼
Phase 8: US6 (Spectator) ───────────────────────────────────► (needs battles/scores)
                                        │
                                        ▼
Phase 9: Blazor UI (optional) ──────────────────────────────► (needs all API endpoints)
                                        │
                                        ▼
Phase 10: Polish ───────────────────────────────────────────► (after all features)
```

### User Story Dependencies

| Story | Depends On | Can Execute After |
|-------|------------|-------------------|
| US1 (Event Creation) | Phase 2 | Phase 2 complete |
| US2 (Judge Scoring) | Phase 2, US1 | US1 complete |
| US5 (Registration) | Phase 2, US1 | US1 complete |
| US3 (Pre-Selection) | US5 | US5 complete |
| US4 (Brackets) | US2, US5 | US2,US5 complete |
| US6 (Spectator) | US2, US4 | US4 complete |

### Parallel Opportunities Within Phases

- **Phase 1**: T002-T005 all parallel
- **Phase 2**: T006-T015 (all entities) parallel; T019-T024 parallel
- **Each User Story**: Tests can run in parallel; DTOs can run in parallel

---

## Parallel Examples

### Phase 2: Entity Creation

```bash
# All entity files can be created simultaneously:
T006: CategoryPhase enum
T007: BattleStatus enum
T008: BracketLevel enum
T009: RegistrationStatus enum
T010: BattleEvent entity
T011: AgeCategory entity
T012: Breaker entity
T013: Registration entity
T014: Battle entity
T015: JudgeScore entity
```

### User Story 2: Tests and DTOs

```bash
# All test files can be created simultaneously:
T039-T045: All US2 test files

# All DTO files can be created simultaneously:
T047-T050: All US2 DTO files
```

---

## Implementation Strategy

### MVP First (Stories US1 + US2)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL)
3. Complete Phase 3: User Story 1 (Event Creation)
4. Complete Phase 4: User Story 2 (Judge Scoring)
5. **STOP and VALIDATE**: Create event, add manual battle, score it, verify winner
6. Deploy MVP if ready

### Incremental Delivery

| Increment | Stories | Capability |
|-----------|---------|------------|
| MVP | US1 + US2 | Events + Manual Scoring |
| +Registration | +US5 | Breaker Registration |
| +Brackets | +US3 + US4 | Full Tournament Flow |
| +Spectators | +US6 | Public Scoreboard |
| +UI | Blazor | User Interface |

### TDD Workflow Per Task

```bash
# For each implementation task:
1. Write test (T0xx test task) → Run → FAIL (Red)
2. Implement minimal code (T0xx impl task) → Run → PASS (Green)
3. Refactor if needed → Run → PASS
4. Commit
```

---

## Summary

| Metric | Count |
|--------|-------|
| **Total Tasks** | 117 |
| **Phase 1 (Setup)** | 5 |
| **Phase 2 (Foundation)** | 19 |
| **US1 Tasks** | 14 |
| **US2 Tasks** | 20 |
| **US5 Tasks** | 12 |
| **US3 Tasks** | 11 |
| **US4 Tasks** | 12 |
| **US6 Tasks** | 12 |
| **Blazor UI Tasks** | 6 |
| **Polish Tasks** | 6 |
| **MVP Scope** | T001-T058 (58 tasks) |

---

## Notes

- All tests follow TDD per Constitution Principle I
- [P] tasks have no file conflicts—safe for parallel execution
- [US?] labels trace tasks to spec.md user stories
- Commit after each task or logical group
- Run `dotnet test` after each implementation task
- Each checkpoint enables independent story validation
