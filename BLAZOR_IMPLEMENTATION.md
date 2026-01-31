# Blazor UI Implementation Plan

## Date: 2026-01-31
## Project: Breaking Battle ScoreBoard - Blazor Web UI

---

## Executive Summary

This document outlines the implementation strategy for building a modern, functionally robust Blazor Web UI for the Breaking Battle ScoreBoard application. The backend API is fully implemented; the focus is on creating a comprehensive, user-friendly UI that leverages all backend capabilities.

---

## Current State Assessment

### ✅ Completed (Backend)
- Full ASP.NET Core API with all controllers
- Entity Framework Core with PostgreSQL
- Domain services (Scoring, PreSelection, Bracket)
- PIN authentication infrastructure
- OpenAPI/Swagger documentation

### ✅ Partially Complete (Frontend)
- Blazor Server configured in Program.cs
- Basic pages exist:
  - Dashboard.razor (Event creation + Event listing with pagination)
  - Scoring.razor (Basic PIN auth + Battle loading + Score submission)
  - Scoreboard.razor (Event selection + Live battles + Recent results)
- Basic components exist:
  - App.razor, Layout.razor
  - ScoreInput.razor, BracketView.razor, CountdownTimer.razor

### ❌ Missing (High Priority)
- UI Component Library integration
- Typed HttpClient services for API calls
- Comprehensive Organizer Dashboard features:
  - Event details view
  - Category management (CRUD)
  - Breaker/Registration management
  - Pre-selection and bracket management
  - Battle management (start, reveal, walkover)
- Enhanced Judge Scoring features
- Full Spectator Scoreboard with real-time updates
- Reusable components (EventCard, BattleCard, Toast, etc.)
- Comprehensive error handling
- Loading/Empty states
- Form validation
- Responsive design polish

---

## Architecture Decisions

### 1. Component Library Selection: **MudBlazor**

**Rationale:**
- Modern Material Design components
- Excellent documentation and community support
- Built specifically for Blazor (not a wrapper)
- Rich component set (Data Grid, Dialogs, Snackbars, etc.)
- Responsive by default
- Active maintenance and .NET 8 support
- Free and open source

**Alternatives Considered:**
- Blazorise: Good but more abstract
- Radzen: Feature-rich but heavier
- Bootstrap (vanilla): Less Blazor-specific, more manual work

### 2. API Client Pattern: **Typed HttpClient Services**

**Pattern:**
```csharp
public class EventsApiClient
{
    private readonly HttpClient _httpClient;
    
    public EventsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<List<EventResponse>> GetEventsAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync("/events", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<EventResponse>>(ct);
    }
}
```

**Registration:**
```csharp
builder.Services.AddHttpClient<EventsApiClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5000"); // From config
});
```

**Benefits:**
- Strongly typed
- Testable
- Centralized error handling
- Reusable across components
- Clear separation of concerns

### 3. State Management: **Component State + Services**

**Strategy:**
- Local component state for UI-specific data
- Shared services for cross-component state (e.g., current event context)
- No complex state management library needed (keep it simple)

### 4. Real-Time Updates: **Polling (30-second intervals)**

**Rationale:**
- SignalR would be ideal but adds complexity
- Polling is simpler and meets requirements
- 30-second refresh is acceptable for spectator view
- Can upgrade to SignalR later if needed

### 5. Error Handling Strategy

**Layers:**
1. **API Client Level**: Catch HttpRequestException, handle status codes
2. **Component Level**: Display user-friendly messages
3. **Global Level**: Catch unhandled exceptions

**User Feedback:**
- Toast notifications for success/info
- Inline alerts for errors
- Loading spinners for async operations

---

## Implementation Tasks (Priority Order)

### Phase 1: Foundation (Tasks T106-T111 base)

#### 1.1 Install and Configure MudBlazor
- [ ] Install MudBlazor NuGet package
- [ ] Configure in Program.cs
- [ ] Add MudBlazor CSS/JS references
- [ ] Create base theme configuration

#### 1.2 Create API Client Services
- [ ] EventsApiClient (GET, POST, PATCH, regenerate PIN)
- [ ] CategoriesApiClient (GET, POST, start-preselection, start-bracket, advance)
- [ ] RegistrationsApiClient (POST, GET registrations for category)
- [ ] BattlesApiClient (GET, POST start/reveal/walkover)
- [ ] ScoresApiClient (POST scores, GET scores for battle)
- [ ] PublicApiClient (GET scoreboard, standings, live battle)

#### 1.3 Create Base Infrastructure
- [ ] PinService for managing PIN state across components
- [ ] ToastService for notifications
- [ ] ErrorHandlingService for consistent error display
- [ ] Configuration service for API base URL

### Phase 2: Reusable Components

#### 2.1 Core Components
- [ ] **EventCard.razor**: Display event summary
- [ ] **BattleCard.razor**: Display battle information
- [ ] **BreakerListComponent.razor**: List breakers with filter/sort
- [ ] **ConfirmDialog.razor**: Confirmation dialogs for dangerous operations
- [ ] **LoadingSpinner.razor**: Centralized loading indicator
- [ ] **EmptyState.razor**: Empty state with icon and message

#### 2.2 Enhance Existing Components
- [ ] **ScoreInput.razor**: Add validation, better UX
- [ ] **BracketView.razor**: Full bracket visualization
- [ ] **CountdownTimer.razor**: Real-time countdown with visual feedback

### Phase 3: Organizer Dashboard Enhancement

#### 3.1 Event Management
- [ ] **Events list view** (already exists, enhance with MudBlazor DataGrid)
- [ ] **Event details view** (new page: /organizer/events/{id})
  - Event info
  - Categories list
  - Quick actions (regenerate PIN, close registration)
  
#### 3.2 Category Management
- [ ] **Create category** form (in event details)
- [ ] **Category details** view
- [ ] **Start pre-selection** button with confirmation
- [ ] **Start bracket** button with confirmation
- [ ] **Advance bracket** button with level selection

#### 3.3 Registration Management
- [ ] **Breakers list** per category
- [ ] **Add breaker** form
- [ ] **Registration status** display
- [ ] **Search/filter** breakers

#### 3.4 Battle Management
- [ ] **Battles list** view per category
- [ ] **Start battle** action
- [ ] **Reveal scores** action
- [ ] **Mark walkover** action
- [ ] **Battle status** indicators

### Phase 4: Judge Scoring Enhancement

#### 4.1 Authentication
- [ ] Improve PIN entry UX
- [ ] Remember authenticated state (session storage)
- [ ] Clear PIN on logout

#### 4.2 Scoring Interface
- [ ] Better judge identifier selection (dropdown with names/numbers)
- [ ] Visual feedback for score submission states
- [ ] Show current battle countdown timer
- [ ] Display score locking status
- [ ] Handle re-battle scenarios

#### 4.3 Battle Navigation
- [ ] List of available battles for scoring
- [ ] Quick battle selection
- [ ] Battle history view

### Phase 5: Spectator Scoreboard Enhancement

#### 5.1 Event Selection
- [ ] Event dropdown (fetch from API)
- [ ] Event search
- [ ] Remember selected event

#### 5.2 Live Display
- [ ] Category filter tabs
- [ ] Bracket progression visualization
- [ ] Countdown timer for reveal
- [ ] Winner announcement animations
- [ ] Real-time score updates (polling)

#### 5.3 Standings View
- [ ] Category standings
- [ ] Breaker rankings
- [ ] Battle history

### Phase 6: Polish & Responsive Design

#### 6.1 Responsive Design
- [ ] Mobile-first layouts
- [ ] Tablet breakpoints
- [ ] Desktop optimizations

#### 6.2 Accessibility
- [ ] ARIA labels
- [ ] Keyboard navigation
- [ ] Screen reader support
- [ ] Focus management

#### 6.3 Performance
- [ ] Lazy loading
- [ ] Component virtualization for long lists
- [ ] Optimize re-renders

#### 6.4 Error Handling
- [ ] Global error boundary
- [ ] Retry logic for failed requests
- [ ] Offline detection

---

## Testing Strategy

### Manual Testing Checklist
- [ ] Create event end-to-end
- [ ] Add categories to event
- [ ] Register breakers
- [ ] Start pre-selection
- [ ] Score battles
- [ ] Advance bracket
- [ ] View spectator scoreboard
- [ ] Test all error scenarios
- [ ] Test responsive design (mobile, tablet, desktop)
- [ ] Test with real data

### Test Scenarios
1. **Happy Path**: Full tournament flow
2. **Error Cases**: Invalid PINs, network errors, validation failures
3. **Edge Cases**: Ties, walkovers, odd number of breakers
4. **Concurrent**: Multiple judges scoring simultaneously

---

## File Structure

```
src/BreakingScoreBoard.Api/
├── Components/
│   ├── Shared/              # Reusable components
│   │   ├── EventCard.razor
│   │   ├── BattleCard.razor
│   │   ├── BreakerList.razor
│   │   ├── ConfirmDialog.razor
│   │   ├── LoadingSpinner.razor
│   │   ├── EmptyState.razor
│   │   └── Toast.razor
│   ├── App.razor
│   ├── Layout.razor
│   ├── ScoreInput.razor      # Enhanced
│   ├── BracketView.razor     # Enhanced
│   └── CountdownTimer.razor  # Enhanced
├── Pages/
│   ├── Organizer/
│   │   ├── Dashboard.razor           # Enhanced
│   │   ├── EventDetails.razor        # New
│   │   ├── CategoryDetails.razor     # New
│   │   └── BattleManagement.razor    # New
│   ├── Judge/
│   │   └── Scoring.razor             # Enhanced
│   └── Spectator/
│       └── Scoreboard.razor          # Enhanced
├── Services/                 # API Clients
│   ├── EventsApiClient.cs
│   ├── CategoriesApiClient.cs
│   ├── RegistrationsApiClient.cs
│   ├── BattlesApiClient.cs
│   ├── ScoresApiClient.cs
│   ├── PublicApiClient.cs
│   ├── PinService.cs
│   └── ApiClientBase.cs      # Shared error handling
├── Models/                   # View Models
│   ├── CreateEventViewModel.cs
│   ├── SubmitScoresViewModel.cs
│   ├── CreateCategoryViewModel.cs
│   ├── RegisterBreakerViewModel.cs
│   └── ...
└── Program.cs                # Configure MudBlazor + Services
```

---

## Implementation Order

### Priority 1: Core Infrastructure (Day 1)
1. Install MudBlazor
2. Create API client services
3. Create base components (LoadingSpinner, EmptyState, Toast)

### Priority 2: Organizer Dashboard (Day 1-2)
4. Enhance event management
5. Implement category management
6. Implement registration management
7. Implement battle management

### Priority 3: Judge & Spectator (Day 2)
8. Enhance judge scoring
9. Enhance spectator scoreboard

### Priority 4: Polish (Day 3)
10. Responsive design
11. Error handling
12. Accessibility
13. Final testing and bug fixes

---

## Success Criteria

### Functional
- ✅ All backend API endpoints are used correctly
- ✅ All CRUD operations work
- ✅ PIN authentication works for admin and judge operations
- ✅ Real-time updates work (polling)
- ✅ Error cases are handled gracefully

### UX
- ✅ Modern, clean design
- ✅ Responsive on mobile, tablet, desktop
- ✅ Loading states for all async operations
- ✅ Clear error messages
- ✅ Success feedback (toasts)
- ✅ Intuitive navigation

### Technical
- ✅ Clean separation of concerns
- ✅ Reusable components
- ✅ Typed API clients
- ✅ No errors in browser console
- ✅ Follows Blazor best practices

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| MudBlazor learning curve | Start with simple components, use documentation |
| API integration issues | Test each API client independently |
| Responsive design complexity | Mobile-first approach, test on real devices |
| Performance with large data | Use virtualization, pagination |
| Time constraints | Prioritize functional over polish |

---

## Notes

- Focus on **functionality first**, then polish the UI
- Keep components small and focused
- Use MudBlazor components where possible to save time
- Test each feature as it's built
- Document any deviations from the plan

---

## References

- [MudBlazor Documentation](https://mudblazor.com/)
- [Blazor Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [HttpClient Best Practices](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines)
- Project spec: `/specs/001-breaking-battles/spec.md`
- Project plan: `/specs/001-breaking-battles/plan.md`
- Tasks: `/specs/001-breaking-battles/tasks.md`

