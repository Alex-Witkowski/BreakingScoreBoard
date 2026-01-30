# Feature Specification: Breaking Battle Management System

**Feature Branch**: `001-breaking-battles`  
**Created**: 2026-01-30  
**Status**: Draft  
**Input**: A web application for managing breaking (breakdance) battles with age categories, pre-selection rounds, knockout brackets (Top32, Top16, etc.), and judge scoring.

## Clarifications

### Session 2026-01-30

- Q: How should ties (equal avg scores) be resolved? → A: Automatic re-battle with same judges; in re-battle, forced differentiation rule applies (each judge MUST score breakers differently—no equal scores within a judge's pair)
- Q: How should pre-selection rounds work when registrations exceed bracket size? → A: Overflow-based random selection; if N breakers over capacity, 2×N random breakers compete in N pre-selection battles; winners join main bracket
- Q: What authentication model for organizers and judges? → A: Simple PIN-based; organizer creates event with admin PIN, system generates separate judge PIN per event; no user accounts needed
- Q: How should score corrections/resubmissions be handled? → A: Judges may resubmit freely until "show results" reveal countdown is triggered; once countdown starts, scores lock and no changes accepted
- Q: How are walkovers (breaker no-shows) handled? → A: System flags battle as "pending walkover"; organizer manually marks winner (no automatic forfeit); battle records walkover status without numeric scores

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Organizer Creates Battle Event with Categories (Priority: P1)

A battle event organizer (competition coordinator) defines a new breaking battle event with age categories and judicial configuration.

**Why this priority**: This is the foundational setup that enables all other features. Without event configuration, judges and breakers cannot participate. An event is the root aggregate in the domain model.

**Independent Test**: Can be fully tested by creating an event, querying it back, and verifying age categories are correctly stored and retrievable without any bracket or participant data.

**Acceptance Scenarios**:

1. **Given** an organizer enters a valid admin PIN, **When** they create a new event with title, date, age categories (U8, U10, U14, U18, Open), and judge count (3 or 5), **Then** the event is persisted, assigned a unique ID, and a separate judge PIN is generated for the event
2. **Given** an existing event, **When** an organizer attempts to modify age categories or judge count after the first battle starts, **Then** the system prevents modification and returns error
3. **Given** an event without age categories, **When** an organizer attempts to save, **Then** the system requires at least one age category with name and upper age limit
4. **Given** an event, **When** queried by ID, **Then** all configured categories and judge settings are returned exactly as saved

---

### User Story 2 - Judge/Official Conducts Battle and Scores Breakers (Priority: P1)

A judge (official) runs a 1v1 battle between two breakers, evaluates their moves, and submits a score for each breaker independently.

**Why this priority**: This is the core value of the system—accurate scoring drives fair competition outcomes. Without this, the bracket system is meaningless. This is the most critical user interaction.

**Independent Test**: Can be fully tested by (a) creating a battle with two breakers pre-populated, (b) having a judge submit independent scores for each breaker, (c) verifying scores are recorded and correctly represent judge assessment.

**Acceptance Scenarios**:

1. **Given** a battle is scheduled with Breaker A vs Breaker B and 3 judges assigned, **When** a judge enters the event's judge PIN and views the active battle, **Then** they see both breakers' names and a scoring interface for each breaker independently
2. **Given** a judge is scoring a battle, **When** they submit a score (0-100 integer) for Breaker A and a different score for Breaker B, **Then** each score is recorded with judge identity, timestamp, and battle reference; judge may resubmit to correct until reveal countdown begins
3. **Given** all judges have submitted scores for a battle, **When** organizer triggers "show results" (or system auto-triggers if configured), **Then** a countdown begins and all scores are locked—no further resubmissions accepted
4. **Given** scores are locked and countdown completes, **When** scores are revealed, **Then** the breaker with higher average score (sum of all judge scores ÷ number of judges) is displayed as winner
5. **Given** a tie occurs (equal average scores), **When** the battle concludes, **Then** the system automatically schedules a re-battle between the same two breakers with the same judges; the re-battle uses forced differentiation: each judge MUST assign different scores to the two breakers (no equal scores allowed within a judge's pair); after the re-battle, winner is determined by highest average score with no further tie handling
6. **Given** a field contains invalid data (negative, >100, non-integer), **When** judge submits, **Then** the system rejects with clear message indicating allowed range

---

### User Story 3 - System Runs Pre-Selection for Overflow Registrations (Priority: P2)

When more breakers register than the bracket size allows (e.g., 36 registered for 32-slot bracket), the system randomly selects overflow breakers for pre-selection battles to reduce the pool to bracket size.

**Why this priority**: Pre-selection enables fair participation when demand exceeds capacity. Without this, organizers must manually reject registrations or run ad-hoc battles. This is simpler than full qualification rounds but handles the common "slightly over capacity" scenario.

**Independent Test**: Can be fully tested by (a) registering 36 breakers in a 32-slot category, (b) triggering pre-selection generation, (c) verifying exactly 8 breakers are randomly selected for 4 pre-selection battles, (d) after winners determined, verifying 32 breakers remain for Top32.

**Acceptance Scenarios**:

1. **Given** 36 breakers registered in a category with 32-slot bracket, **When** organizer triggers pre-selection, **Then** system calculates overflow (36-32=4), selects 8 random breakers (2×overflow), and creates 4 pre-selection battles
2. **Given** pre-selection battles are created, **When** all 4 battles are scored and winners determined, **Then** the 4 winners join the main bracket and 4 losers are eliminated, leaving exactly 32 breakers for Top32
3. **Given** 32 or fewer breakers registered, **When** organizer attempts to trigger pre-selection, **Then** system reports "No pre-selection needed" and proceeds directly to bracket generation
4. **Given** pre-selection is in progress, **When** a new registration arrives, **Then** system rejects with "Registration closed - pre-selection in progress"
5. **Given** 40 breakers registered for 32-slot bracket (overflow=8), **When** pre-selection triggers, **Then** 16 random breakers compete in 8 pre-selection battles, 8 winners advance to Top32 with the 24 non-selected breakers

---

### User Story 4 - System Progresses Breakers Through Knockout Brackets (Priority: P2)

The system automatically advances breakers who won their battles to the next bracket level (Top32 → Top16 → Top8 → Top4 → Final).

**Why this priority**: Bracket progression is critical for tournament flow but depends on Story 2 (battle scoring). Once battles are scored, progression is deterministic and can be done automatically, reducing manual organizer work.

**Independent Test**: Can be fully tested by (a) creating a bracket structure, (b) simulating multiple battles with scores, (c) triggering bracket advancement, (d) verifying winners are correctly placed in next round without any UI/judge involvement.

**Acceptance Scenarios**:

1. **Given** 32 breakers in an age category all have completed their Top32 battles with winners determined, **When** organizer triggers bracket advancement, **Then** the 16 winning breakers are automatically matched into 8 Top16 battles
2. **Given** a breaker advanced to Top16, **When** they win their Top16 battle, **Then** they are added to Top8 pending battles and a new battle pairing is created
3. **Given** bracket advancement occurs, **When** querying an age category standings, **Then** breakers are correctly listed by their current bracket level (Top32, Top16, Top8, etc.) with win/loss records
4. **Given** uneven winners (e.g., 9 winners, bracket needs 8), **When** system creates next round pairings, **Then** a randomly-selected winning breaker receives a bye (advances directly to next round without a battle) and 8 battles are created for remaining winners
5. **Given** a breaker's previous battle result is corrected (score changed), **When** bracket recalculation is triggered, **Then** bracket standings are recalculated and affected breakers notified

---

### User Story 5 - Breaker Registers for Battle in Age Category (Priority: P2)

A breaker (participant) registers for a battle event in their age-appropriate category.

**Why this priority**: Registration is essential for populating brackets but can be implemented independently of judge scoring. It provides organizers a clear participant list before battles begin.

**Independent Test**: Can be fully tested by creating a registration request, storing it, and verifying the breaker appears in age category participant lists without any battle or bracket logic running.

**Acceptance Scenarios**:

1. **Given** an event is open for registration, **When** a breaker submits (name, age, category selection), **Then** they are registered and assigned a unique participant ID unless they already registered
2. **Given** a breaker's calculated age does not fit the selected category (e.g., age 15 selecting U14), **When** they attempt registration, **Then** the system rejects and suggests valid categories based on age
3. **Given** registration deadline has passed, **When** a new registration request arrives, **Then** the system rejects with "Registration Closed" message
4. **Given** a breaker registered twice (accidental duplicate), **When** system detects, **Then** the newer registration replaces the older one (idempotent)

---

### User Story 6 - Spectators View Live Battle Scores and Standings (Priority: P3)

Spectators (audience, organizers, external observers) view active battle scores, winner announcements, and current bracket standings in real-time.

**Why this priority**: Display of scores enhances viewer experience and keeps audiences engaged but is not required for accurate competition management. Can be added after core battle/bracket logic is stable.

**Independent Test**: Can be fully tested by (a) populating battles with scores from Story 2, (b) querying public standings endpoints, (c) verifying data is current and formatted for display without any authentication/authorization requirements beyond read-only access.

**Acceptance Scenarios**:

1. **Given** a battle has been scored by judges, **When** a spectator views the live scoreboard, **Then** they see final score (winner name, avg score, opponent name, avg score) within 5 seconds of judge submission
2. **Given** multiple categories are running concurrently, **When** a spectator filters standings by age category, **Then** they see only breakers and battles for that category
3. **Given** brackets in progress, **When** a spectator views standings, **Then** current round (Top32, Top16, etc.) is clearly labeled with pending and completed battles
4. **Given** no authentication required for viewing, **When** public standings endpoint is accessed, **Then** it returns current bracket state anonymously

### Edge Cases

- What happens when a judge submits a score, then resubmits before reveal countdown? → Allowed; latest score used
- What happens when a judge tries to resubmit after reveal countdown started? → Rejected; original score stands
- How does the system handle concurrent submissions from multiple judges scoring the same battle simultaneously? → All accepted independently until reveal lock
- How are walkovers (breaker no-shows) recorded and handled in bracket progression? → Organizer flags battle as walkover, manually marks winner; no scores recorded; winner advances with "W.O." status
- What if a battle is canceled after scores are submitted but before bracket advancement?
- How does the system handle brackets with an odd number of winners (e.g., 9 winners needing 8 Top16 slots)?
- What happens if the event organizer disqualifies a breaker mid-tournament after they've already won battles?
- Can a battle be re-scored if judges request correction after aggregate score is published?
- What if database connection fails during bracket advancement—can the operation be retried safely?
- How are walkovers (breaker no-shows) recorded and handled in bracket progression?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow organizers to create a battle event with name, date, configurable age categories (each with name and upper age limit), location, and judge count (3 or 5)
- **FR-002**: System MUST validate that each breaker's calculated age falls within exactly one age category at registration time
- **FR-003**: System MUST store and retrieve up to 32 breakers per age category in a single event without data loss or duplication
- **FR-004**: System MUST present judges with a scoring interface showing two breakers' names and independent score input fields (0-100 integer range)
- **FR-005**: System MUST persist individual judge scores with timestamp, judge identity, breaker identity, and battle reference for audit trails
- **FR-006**: System MUST calculate battle winner as the breaker with highest average score (sum ÷ judge count), with tie detection
- **FR-007**: System MUST prevent score submission outside the valid range (0-100) with clear error messaging
- **FR-008**: System MUST automatically advance winning breakers from one bracket level to the next (e.g., Top32 → Top16) after batch advancement trigger
- **FR-009**: System MUST handle odd-number bracket situations (e.g., 9 winners → 8 slots) by applying random bye selection
- **FR-016**: System MUST calculate pre-selection overflow as (registered_count - bracket_size) when registrations exceed bracket capacity
- **FR-017**: System MUST randomly select (2 × overflow) breakers to compete in pre-selection battles when overflow > 0
- **FR-018**: System MUST create (overflow) pre-selection battles, with winners advancing to main bracket and losers eliminated
- **FR-019**: System MUST block new registrations once pre-selection battles have been generated
- **FR-020**: System MUST use PIN-based authentication: organizer creates event with admin PIN, system generates separate judge PIN for that event
- **FR-021**: System MUST require valid judge PIN to access scoring interface; judge identity tracked by session/device identifier within event
- **FR-022**: System MUST allow organizer to regenerate judge PIN if compromised, invalidating previous PIN immediately
- **FR-023**: System MUST support "show results" reveal: organizer manually triggers or system auto-triggers when all judges have scored
- **FR-024**: System MUST lock all scores for a battle once reveal countdown begins; resubmissions after lock are rejected
- **FR-025**: System MUST allow judges to resubmit scores freely before reveal countdown starts (latest submission wins)
- **FR-026**: System MUST display countdown timer to spectators before revealing winner and individual judge scores
- **FR-027**: System MUST allow organizer to flag a battle as "walkover" when a breaker does not show; organizer manually selects winner
- **FR-028**: System MUST record walkover battles with winner but without numeric scores; walkover status visible in standings and bracket display
- **FR-010**: System MUST expose public (unauthenticated) read-only endpoints returning current battle scores, winner announcements, and bracket standings in JSON format
- **FR-011**: System MUST log all scoring events with context (judge ID, breaker IDs, scores, timestamp, user agent) for audit and debugging
- **FR-012**: System MUST reject duplicate registrations (same breaker in same category) and handle gracefully (replace or error based on state)
- **FR-013**: System MUST prevent organizer modifications to event age categories or judge count after first battle has been scheduled
- **FR-014**: System MUST support idempotent bracket advancement (running advancement twice produces same result as running once)
- **FR-015**: System MUST provide organizer dashboards showing current event state: registered breakers per category, completed/pending battles, bracket progression

### Key Entities

- **Battle Event**: Container for a competition; references multiple age categories, scheduling info, judge configuration, and organizer
- **Age Category**: Defines an age range (e.g., "U14" = under 14 years old) within an event; contains multiple registered breakers
- **Breaker**: A participant; has name, birth date (for age calculation), and registration records in specific categories/events
- **Battle**: A 1v1 match between two specific breakers; records two participants, assigned judges, scheduled/completed status
- **Judge Score**: Individual score submission; records judge ID, breaker ID, score value (0-100), timestamp, battle reference
- **Bracket Level**: A tournament stage (Top32, Top16, Top8, etc.); aggregates all battles at that level for a specific category
- **Tournament Stand**: Read-only aggregate showing current standings: breaker, current bracket level, wins, losses, avg score

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Event organizers can create and configure a new event with 5+ age categories and judging setup in under 10 minutes without documentation
- **SC-002**: Judges can view an active battle and submit independent scores for two breakers in under 30 seconds from page load
- **SC-003**: System calculates battle winners with 100% accuracy (no scoring errors or incorrect winner determination from judge inputs)
- **SC-004**: After all battles in a bracket level complete, winning breakers are automatically advanced to the next level within 5 seconds of advancement trigger
- **SC-005**: Bracket standings are queryable and return current state (breaker name, current round, wins, avg score) within 1 second for up to 32 breakers per category
- **SC-006**: System supports simultaneous scoring of up to 5 different battles (5 judges in parallel) without data corruption or race conditions
- **SC-007**: Score audit trail logs 100% of judge submissions with judge ID, timestamp, and exact score values for at least 90 days
- **SC-008**: Public scoreboard updates within 5 seconds of judge score submission, showing accurate winner and final score
- **SC-009**: System prevents score entry outside 0-100 range and rejects with clear error message 100% of the time
- **SC-010**: Organizers report 90% task completion rate for: creating events, viewing standings, triggering bracket advancement without errors
- **SC-011**: System handles registration of 32 breakers per category with no duplicate entries or data loss
- **SC-012**: Bracket advancement is idempotent—running twice produces identical results to running once
