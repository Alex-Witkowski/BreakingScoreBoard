# MudBlazor Quick Start Prompts

Common prompts for MudBlazor development tasks in this project.

## Setup and Configuration

### Initialize a new MudBlazor page
```
Create a new interactive Blazor page at /organizer/battles with MudBlazor components. 
Include a data table showing battles with columns for ID, Date, Category, and Status.
Add filter and sort capabilities.
```

### Add MudBlazor dialog
```
Create a MudBlazor dialog component for editing a BattleEvent. 
Include form validation and use MudTextField, MudDatePicker, and MudSelect components.
Handle both create and edit modes.
```

## Common Components

### Data Grid
```
Create a MudDataGrid to display events with:
- Columns: Title, Location, Date, Judge Count, Status
- Sortable and filterable
- Row click navigation to event details
- Action buttons for edit and delete
```

### Form with Validation
```
Build a MudBlazor form to create a new category with:
- Name (required, max 50 chars)
- Max Age (required, numeric)
- Bracket Size (select: 4, 8, 16, 32)
- Data annotations validation
- Success/error notifications via snackbar
```

### Navigation Drawer
```
Add a MudDrawer navigation menu with:
- Dashboard
- Events
- Categories  
- Registrations
- Settings
Make it responsive with temporary drawer on mobile
```

## Troubleshooting

### Fix render mode error
```
I'm getting "Cannot pass parameter 'ChildContent'" error with MudBlazor.
Help me fix the render mode configuration using the InteractiveWrapper pattern.
```

### Fix provider errors
```
Components are showing "Missing MudPopoverProvider" errors.
Check my InteractiveWrapper.razor and ensure all 4 providers are included.
```

### Fix static SSR issue
```
MudBlazor components aren't responding to clicks on my page.
Add the @rendermode InteractiveServer directive and verify the setup.
```

## Advanced Patterns

### Master-Detail View
```
Create a master-detail page showing:
- Left: MudList of events
- Right: Selected event details with tabs for Categories, Registrations, Battles
- Update details when list item is clicked
```

### Real-time Updates
```
Implement real-time score updates using SignalR and MudBlazor:
- Subscribe to score changes
- Update MudDataGrid automatically
- Show notifications for new scores
```

### Loading States
```
Add loading states to the dashboard:
- Skeleton loaders for initial data
- Progress indicators during operations
- Disable buttons during async operations
```

## Best Practices

### Accessibility Check
```
Review this MudBlazor form for accessibility:
- Verify all inputs have labels
- Check color contrast
- Ensure keyboard navigation works
- Add aria-labels where needed
```

### Performance Optimization
```
Optimize this MudDataGrid displaying 1000+ items:
- Add virtualization
- Implement pagination
- Add debouncing to search/filter
- Use @key for list items
```

## Testing

### Component Test
```
Create a unit test for the Dashboard.razor component:
- Mock EventsApiClient
- Test event loading
- Verify form submission
- Check error handling
```

## Tips for Using These Prompts

1. **Be specific** - Include component names, file paths, and data models
2. **Reference existing code** - Mention similar pages or components
3. **State requirements** - Specify interactivity, validation, styling needs
4. **Ask for explanations** - Add "Explain why..." to understand the approach

## Example Workflows

### Creating a New Feature Page

1. "Create a new Blazor page for judge scoring at /judge/scoring with @rendermode InteractiveServer"
2. "Add a MudGrid layout with battle info on left, score input on right"
3. "Create MudSlider components for each scoring category (Technique, Creativity, etc.)"
4. "Add submit button that calls ScoresApiClient and shows success notification"
5. "Handle validation - ensure all scores 0-10, battle exists, judge authorized"

### Debugging a MudBlazor Issue

1. "I see 'Missing MudPopoverProvider' in browser console. Check InteractiveWrapper.razor"
2. "Verify all 4 MudBlazor providers are present and @rendermode InteractiveServer is set"
3. "Check that Layout.razor includes <InteractiveWrapper /> before MudLayout"
4. "Ensure page has @rendermode InteractiveServer directive"
5. "Test with dotnet run and navigate to the problematic page"
