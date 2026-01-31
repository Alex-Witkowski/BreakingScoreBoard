# BreakingScoreBoard - GitHub Copilot Instructions

This file provides context and guidance for GitHub Copilot when working on the BreakingScoreBoard project.

## Project Overview

BreakingScoreBoard is a .NET 9 Blazor Web App for managing breaking (breakdancing) battle competitions. It uses:
- **ASP.NET Core 9.0** with Blazor Server (Interactive Server render mode)
- **MudBlazor 8.15.0** for UI components
- **PostgreSQL** with Entity Framework Core for data persistence
- **SignalR** for real-time updates

## Technology Stack

### Frontend
- Blazor Server with per-page/component interactivity
- MudBlazor Material Design components
- Custom CSS for competition-specific styling

### Backend
- ASP.NET Core Web API controllers
- Entity Framework Core with PostgreSQL
- Domain services in `BreakingScoreBoard.Domain`

## MudBlazor Development Guidelines

### Render Modes (.NET 9 Blazor)

**CRITICAL**: MudBlazor does NOT support static SSR rendering. All pages using MudBlazor components must use interactive render modes.

#### Recommended Pattern for Per-Page Interactivity

1. **Provider Setup** - Create a provider component without ChildContent:

```razor
@* Components/InteractiveWrapper.razor *@
@rendermode InteractiveServer

<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />
```

2. **Layout Integration** - Include provider in layout (NOT wrapping content):

```razor
@inherits LayoutComponentBase

<InteractiveWrapper />

<MudLayout>
    @Body
</MudLayout>
```

3. **Page Interactivity** - Add render mode to interactive pages:

```razor
@page "/my-page"
@rendermode InteractiveServer
```

4. **Imports** - Add to `_Imports.razor`:

```razor
@using static Microsoft.AspNetCore.Components.Web.RenderMode
```

### Common Patterns

#### Do ✅

```razor
@* Interactive page with MudBlazor components *@
@page "/organizer/dashboard"
@rendermode InteractiveServer

<MudPaper>
    <MudTextField @bind-Value="model.Title" Label="Event Title" />
    <MudButton OnClick="@Submit">Submit</MudButton>
</MudPaper>
```

#### Don't ❌

```razor
@* Static SSR page - MudBlazor will fail *@
@page "/static-page"

<MudPaper>  @* This will cause JavaScript interop errors *@
    <MudButton>Click Me</MudButton>
</MudPaper>
```

#### Don't ❌

```razor
@* Wrapper with ChildContent parameter *@
@rendermode InteractiveServer

<MudThemeProvider />
@ChildContent  @* RenderFragment cannot be serialized! *@

@code {
    [Parameter]
    public RenderFragment? ChildContent { get; set; }  @* ERROR *@
}
```

### MudBlazor Component Usage

#### Forms
```razor
<EditForm Model="@model" OnValidSubmit="@HandleSubmit">
    <DataAnnotationsValidator />
    
    <MudTextField @bind-Value="model.Name" 
                  Label="Name" 
                  Variant="Variant.Outlined"
                  Required="true" />
    
    <MudSelect @bind-Value="model.Category" Label="Category">
        <MudSelectItem Value="@("A")">Category A</MudSelectItem>
        <MudSelectItem Value="@("B")">Category B</MudSelectItem>
    </MudSelect>
    
    <MudButton ButtonType="ButtonType.Submit" 
               Variant="Variant.Filled" 
               Color="Color.Primary">
        Submit
    </MudButton>
</EditForm>
```

#### Dialogs
```razor
@inject IDialogService DialogService

<MudButton OnClick="@OpenDialog">Open Dialog</MudButton>

@code {
    private async Task OpenDialog()
    {
        var parameters = new DialogParameters { ["Item"] = myItem };
        var options = new DialogOptions { CloseOnEscapeKey = true };
        
        var dialog = await DialogService.ShowAsync<MyDialog>("Title", parameters, options);
        var result = await dialog.Result;
        
        if (!result.Canceled)
        {
            // Handle result
        }
    }
}
```

#### Snackbars
```razor
@inject ISnackbar Snackbar

@code {
    private void ShowSuccess()
    {
        Snackbar.Add("Operation successful!", Severity.Success);
    }
    
    private void ShowError(string message)
    {
        Snackbar.Add($"Error: {message}", Severity.Error);
    }
}
```

## Project Structure

```
src/
├── BreakingScoreBoard.Api/          # Main Blazor Server app
│   ├── Components/                   # Razor components
│   │   ├── Layout.razor             # Main layout with MudBlazor
│   │   ├── InteractiveWrapper.razor # MudBlazor providers
│   │   └── _Imports.razor           # Component imports
│   ├── Pages/                       # Routable pages
│   │   ├── Judge/                   # Judge-specific pages
│   │   ├── Organizer/               # Organizer dashboard
│   │   └── Spectator/               # Public scoreboard
│   ├── Controllers/                 # API endpoints
│   ├── Infrastructure/              # Middleware, DbContext
│   ├── Services/                    # API clients
│   └── Program.cs                   # App configuration
├── BreakingScoreBoard.Domain/       # Business logic
│   ├── Entities/                    # EF Core entities
│   └── Services/                    # Domain services
└── BreakingScoreBoard.Tests/        # Test project
```

## Coding Standards

### C# Conventions
- Use C# 13 features where appropriate
- Nullable reference types enabled
- XML documentation for public APIs
- Async/await for I/O operations

### API Design
- RESTful endpoints with proper HTTP verbs
- Structured logging with correlation IDs
- PIN-based authorization for sensitive operations
- Consistent error response format

### Entity Framework
- Use migrations for schema changes
- Navigation properties for relationships
- Indexes on frequently queried fields

### Logging
```csharp
// Use structured logging with correlation ID
_logger.LogInformation(
    "Creating event {EventTitle} on {EventDate}",
    request.Title,
    request.EventDate
);
```

## Testing

- Unit tests in `BreakingScoreBoard.Tests/Unit/`
- Integration tests in `BreakingScoreBoard.Tests/Integration/`
- Use xUnit framework
- Mock external dependencies

## Common Pitfalls to Avoid

1. **RenderFragment Serialization**: Never add ChildContent parameters to components with `@rendermode`
2. **Static SSR with MudBlazor**: All MudBlazor components require interactive render mode
3. **Layout Render Modes**: Layouts cannot have `@rendermode` directive in .NET 9
4. **Missing Providers**: Ensure all 4 MudBlazor providers are present (Theme, Popover, Dialog, Snackbar)
5. **Scoped Services**: Be aware of circuit lifecycle with Interactive Server mode

## Environment Setup

- .NET 9 SDK required
- PostgreSQL database (connection string in appsettings)
- Visual Studio Code or Visual Studio 2022+
- Dev Container support available

## Useful Commands

```bash
# Run the application
dotnet run --project src/BreakingScoreBoard.Api

# Create migration
dotnet ef migrations add MigrationName --project src/BreakingScoreBoard.Api

# Run tests
dotnet test

# Format code
dotnet format
```

## References

- [MudBlazor Documentation](https://mudblazor.com)
- [Blazor Render Modes (.NET 9)](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
