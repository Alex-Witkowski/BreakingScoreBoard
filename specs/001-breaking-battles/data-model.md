# Data Model: Breaking Battle Management System

**Feature**: 001-breaking-battles  
**Date**: 2026-01-30  
**Purpose**: Define entities, relationships, validation rules, and state transitions

## Entity Relationship Diagram (Conceptual)

```
┌─────────────────┐       ┌─────────────────┐
│  BattleEvent    │──────<│   AgeCategory   │
│  (root)         │ 1   * │                 │
└────────┬────────┘       └────────┬────────┘
         │                         │
         │ 1                       │ *
         ▼                         ▼
┌─────────────────┐       ┌─────────────────┐
│  (PINs stored   │       │   Registration  │──────>│ Breaker │
│   on Event)     │       │   (join table)  │  *  1 │         │
└─────────────────┘       └────────┬────────┘       └─────────┘
                                   │
                                   │ *
                                   ▼
                          ┌─────────────────┐
                          │     Battle      │
                          │  (1v1 match)    │
                          └────────┬────────┘
                                   │ 1
                                   │
                                   ▼ *
                          ┌─────────────────┐
                          │   JudgeScore    │
                          │  (per breaker)  │
                          └─────────────────┘
```

---

## Entities

### BattleEvent (Aggregate Root)

The root entity representing a competition event. All other entities are scoped to an event.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | `Guid` | PK, required | Unique identifier |
| `Title` | `string` | required, max 200 | Event name (e.g., "Berlin Breaking Championship 2026") |
| `EventDate` | `DateOnly` | required | Date of the competition |
| `Location` | `string` | optional, max 500 | Venue name/address |
| `JudgeCount` | `int` | required, 3 or 5 | Number of judges per battle |
| `AdminPinHash` | `string` | required | SHA256 hash of organizer PIN |
| `JudgePinHash` | `string` | required | SHA256 hash of judge access PIN |
| `RegistrationOpen` | `bool` | default true | Whether new registrations accepted |
| `CreatedAt` | `DateTime` | required | UTC timestamp |
| `UpdatedAt` | `DateTime` | required | UTC timestamp |

**Validation Rules**:
- `JudgeCount` must be 3 or 5 (FR-001)
- `AdminPinHash` and `JudgePinHash` must be different
- Event cannot be modified (categories, judge count) after first battle scheduled (FR-013)

**Navigation**:
- `Categories`: `ICollection<AgeCategory>` (1:many)

---

### AgeCategory

Defines an age bracket within an event.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | `Guid` | PK, required | Unique identifier |
| `EventId` | `Guid` | FK, required | Parent event |
| `Name` | `string` | required, max 50 | Display name (e.g., "U14", "Open") |
| `MaxAge` | `int?` | optional, 1-99 | Upper age limit (null = no limit, e.g., "Open") |
| `BracketSize` | `int` | required, power of 2 | Target bracket size (16, 32, 64) |
| `CurrentPhase` | `CategoryPhase` | required | Registration, PreSelection, Bracket, Completed |
| `SortOrder` | `int` | required | Display order |

**Validation Rules**:
- `MaxAge` must be > 0 if specified
- `BracketSize` must be 8, 16, 32, or 64
- At least one category required per event (FR-001, US1.3)
- `Name` unique within event

**Navigation**:
- `Event`: `BattleEvent` (many:1)
- `Registrations`: `ICollection<Registration>` (1:many)
- `Battles`: `ICollection<Battle>` (1:many)

---

### Breaker

A participant who competes in battles. Breakers exist independently of events.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | `Guid` | PK, required | Unique identifier |
| `Name` | `string` | required, max 100 | Display name |
| `BirthDate` | `DateOnly` | required | For age calculation |
| `CreatedAt` | `DateTime` | required | UTC timestamp |

**Validation Rules**:
- `BirthDate` must be in past
- Age calculated as: `EventDate.Year - BirthDate.Year` (adjusted for month/day)

**Navigation**:
- `Registrations`: `ICollection<Registration>` (1:many)

---

### Registration (Join Table)

Links a breaker to an age category within an event.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | `Guid` | PK, required | Unique identifier |
| `BreakerId` | `Guid` | FK, required | The participant |
| `CategoryId` | `Guid` | FK, required | The age category |
| `RegisteredAt` | `DateTime` | required | UTC timestamp |
| `Seed` | `int?` | optional | Seeding rank (if pre-seeded) |
| `Status` | `RegistrationStatus` | required | Active, Eliminated, Advanced, Disqualified |

**Validation Rules**:
- Breaker's age must fit category's `MaxAge` at event date (FR-002)
- Unique constraint on (`BreakerId`, `CategoryId`) - no duplicate registrations (FR-012)
- Cannot add registration if `CurrentPhase` != Registration (FR-019)

**Navigation**:
- `Breaker`: `Breaker` (many:1)
- `Category`: `AgeCategory` (many:1)

---

### Battle

A 1v1 match between two breakers.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | `Guid` | PK, required | Unique identifier |
| `CategoryId` | `Guid` | FK, required | Parent category |
| `BracketLevel` | `BracketLevel` | required | PreSelection, Top32, Top16, Top8, Top4, Final |
| `BracketPosition` | `int` | required | Position in bracket (1-based) |
| `Breaker1Id` | `Guid` | FK, required | First competitor |
| `Breaker2Id` | `Guid` | FK, required | Second competitor |
| `WinnerId` | `Guid?` | FK, optional | Winner (null until completed) |
| `Status` | `BattleStatus` | required | Scheduled, InProgress, RevealCountdown, Completed, Walkover |
| `IsReBattle` | `bool` | default false | True if this is a tie re-battle |
| `OriginalBattleId` | `Guid?` | FK, optional | Reference to original battle (if re-battle) |
| `ScheduledAt` | `DateTime?` | optional | When battle is scheduled to start |
| `CompletedAt` | `DateTime?` | optional | When battle was completed |

**Validation Rules**:
- `Breaker1Id` != `Breaker2Id`
- Both breakers must be registered in the category
- `WinnerId` must be either `Breaker1Id` or `Breaker2Id` when set
- Status transitions: Scheduled → InProgress → RevealCountdown → Completed
- Walkover status bypasses scoring (winner set directly by organizer)

**Navigation**:
- `Category`: `AgeCategory` (many:1)
- `Breaker1`: `Breaker` (many:1)
- `Breaker2`: `Breaker` (many:1)
- `Winner`: `Breaker?` (many:1)
- `Scores`: `ICollection<JudgeScore>` (1:many)
- `OriginalBattle`: `Battle?` (self-reference for re-battles)

---

### JudgeScore

Individual score submitted by a judge for one breaker in a battle.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | `Guid` | PK, required | Unique identifier |
| `BattleId` | `Guid` | FK, required | Parent battle |
| `BreakerId` | `Guid` | FK, required | Which breaker this score is for |
| `JudgeIdentifier` | `string` | required, max 100 | Session/device ID (not a user account) |
| `Score` | `int` | required, 0-100 | The score value |
| `SubmittedAt` | `DateTime` | required | UTC timestamp |
| `IsLocked` | `bool` | default false | True after reveal countdown starts |

**Validation Rules**:
- `Score` must be 0-100 (FR-007)
- In re-battles with forced differentiation: judge's scores for Breaker1 and Breaker2 must differ
- Cannot modify score if `IsLocked` = true (FR-024)
- `JudgeIdentifier` + `BattleId` + `BreakerId` should allow resubmission (latest wins until locked)

**Navigation**:
- `Battle`: `Battle` (many:1)
- `Breaker`: `Breaker` (many:1)

---

## Enumerations

### CategoryPhase
```csharp
public enum CategoryPhase
{
    Registration,    // Accepting breaker registrations
    PreSelection,    // Running overflow pre-selection battles
    Bracket,         // Main knockout bracket in progress
    Completed        // All battles finished, champion determined
}
```

### BattleStatus
```csharp
public enum BattleStatus
{
    Scheduled,       // Battle created, waiting to start
    InProgress,      // Judges are scoring
    RevealCountdown, // Scores locked, countdown running
    Completed,       // Winner determined
    Walkover         // One breaker no-show, winner set by organizer
}
```

### BracketLevel
```csharp
public enum BracketLevel
{
    PreSelection = 0,
    Top64 = 64,
    Top32 = 32,
    Top16 = 16,
    Top8 = 8,
    Top4 = 4,      // Semi-finals
    Final = 2
}
```

### RegistrationStatus
```csharp
public enum RegistrationStatus
{
    Active,          // In the competition
    Eliminated,      // Lost in bracket
    Advanced,        // Won the category (champion)
    Disqualified     // Removed by organizer
}
```

---

## State Transitions

### Battle State Machine

```
                    ┌──────────────┐
                    │  Scheduled   │
                    └──────┬───────┘
                           │ [organizer starts battle]
                           ▼
                    ┌──────────────┐
         ┌─────────│  InProgress  │─────────┐
         │         └──────┬───────┘         │
         │                │                 │
         │ [organizer     │ [all judges     │ [organizer marks
         │  marks         │  scored AND     │  walkover]
         │  walkover]     │  reveal         │
         │                │  triggered]     │
         │                ▼                 │
         │         ┌──────────────┐         │
         │         │RevealCountdown│        │
         │         └──────┬───────┘         │
         │                │ [countdown      │
         │                │  completes]     │
         │                ▼                 │
         │         ┌──────────────┐         │
         │         │  Completed   │◄────────┘
         │         └──────┬───────┘
         │                │ [if tie → create re-battle]
         │                ▼
         │         ┌──────────────┐
         └────────>│   Walkover   │
                   └──────────────┘
```

### Category Phase Transitions

```
Registration ──[organizer triggers]──> PreSelection (if overflow)
     │                                        │
     │ [no overflow]                          │ [all pre-selection complete]
     ▼                                        ▼
Bracket ◄─────────────────────────────────────┘
     │
     │ [final battle completed]
     ▼
Completed
```

---

## Aggregated Views (Read Models)

### TournamentStanding

Not persisted; computed on query for spectator display.

| Field | Description |
|-------|-------------|
| `BreakerId` | Breaker identifier |
| `BreakerName` | Display name |
| `CategoryName` | Age category |
| `CurrentLevel` | Last bracket level participated |
| `Wins` | Number of battles won |
| `Losses` | Number of battles lost |
| `AverageScore` | Mean of all received scores (excludes walkovers) |
| `Status` | Active / Eliminated / Champion |

### BattleResult

Computed after battle completion for display.

| Field | Description |
|-------|-------------|
| `BattleId` | Battle identifier |
| `Breaker1Name` | First competitor |
| `Breaker2Name` | Second competitor |
| `Breaker1AvgScore` | Average of all judge scores for breaker 1 |
| `Breaker2AvgScore` | Average of all judge scores for breaker 2 |
| `WinnerName` | Winner's name |
| `IsWalkover` | True if decided by walkover |
| `IndividualScores` | List of (JudgeIdentifier, Breaker1Score, Breaker2Score) |

---

## Database Indexes

| Table | Index | Columns | Purpose |
|-------|-------|---------|---------|
| `BattleEvents` | PK | `Id` | Primary key |
| `AgeCategories` | PK | `Id` | Primary key |
| `AgeCategories` | IX_Event | `EventId` | Query categories by event |
| `Breakers` | PK | `Id` | Primary key |
| `Registrations` | PK | `Id` | Primary key |
| `Registrations` | UX_BreakerCategory | `BreakerId, CategoryId` | Prevent duplicates |
| `Registrations` | IX_Category | `CategoryId` | Query registrations by category |
| `Battles` | PK | `Id` | Primary key |
| `Battles` | IX_Category | `CategoryId` | Query battles by category |
| `Battles` | IX_CategoryLevel | `CategoryId, BracketLevel` | Query bracket standings |
| `JudgeScores` | PK | `Id` | Primary key |
| `JudgeScores` | IX_Battle | `BattleId` | Query scores for battle |
| `JudgeScores` | IX_BattleJudgeBreaker | `BattleId, JudgeIdentifier, BreakerId` | Upsert on resubmission |
