# GitHub Copilot Configuration for BreakingScoreBoard

This directory contains configuration files to enhance GitHub Copilot's understanding of the BreakingScoreBoard project, particularly for MudBlazor and .NET 9 Blazor development.

## Files in This Directory

### `copilot-instructions.md`
General project instructions that apply to all GitHub Copilot interactions within this workspace. This file provides:
- Project overview and technology stack
- MudBlazor render mode patterns
- Coding standards and best practices
- Common pitfalls to avoid

**Usage**: Automatically loaded by GitHub Copilot when working in this workspace.

### `agents/mudblazor-dev.agent.md`
A custom agent profile specialized for MudBlazor development tasks. This agent has deep knowledge of:
- MudBlazor component library
- .NET 9 Blazor render modes
- The InteractiveWrapper pattern
- Common MudBlazor issues and solutions

**Usage**: 
1. Open GitHub Copilot Chat
2. Type `@mudblazor-dev` to invoke the specialized agent
3. Ask MudBlazor-specific questions

Example:
```
@mudblazor-dev How do I create a dialog with form validation?
```

### `prompts/mudblazor-quickstart.md`
A collection of ready-to-use prompts for common MudBlazor development scenarios. Use these as templates for your own questions.

**Usage**: Reference these prompts when you need to:
- Create new MudBlazor components
- Debug render mode issues
- Implement common patterns
- Optimize performance

## Getting Started

### Prerequisites
- GitHub Copilot subscription (Free, Pro, or Enterprise)
- Visual Studio Code with GitHub Copilot extensions installed
  - [GitHub Copilot](https://marketplace.visualstudio.com/items?itemName=GitHub.copilot)
  - [GitHub Copilot Chat](https://marketplace.visualstudio.com/items?itemName=GitHub.copilot-chat)

### Quick Start

1. **Open this workspace** in Visual Studio Code
2. **Verify Copilot is active** - Look for the Copilot icon in the status bar
3. **Test the configuration**:
   ```
   Ask Copilot: "How do I set up a new MudBlazor page with InteractiveServer render mode?"
   ```
4. **Try the custom agent**:
   ```
   @mudblazor-dev Create a data table for displaying events
   ```

## Common Use Cases

### 🎯 Creating New Features

When building new pages or components:

1. **Ask about patterns first**:
   ```
   What's the recommended way to create a master-detail view with MudBlazor?
   ```

2. **Request implementation**:
   ```
   Create a new Blazor page at /organizer/categories that lists all categories
   in a MudDataGrid with edit and delete buttons.
   ```

3. **Add interactivity**:
   ```
   @mudblazor-dev Add a dialog for editing category details with form validation
   ```

### 🐛 Debugging Issues

When encountering errors:

1. **Describe the error**:
   ```
   I'm getting "Cannot pass parameter 'ChildContent' to component" error. 
   What's wrong with my InteractiveWrapper?
   ```

2. **Use the custom agent**:
   ```
   @mudblazor-dev My MudButton isn't responding to clicks. Help me debug.
   ```

3. **Check patterns**:
   ```
   Show me the correct InteractiveWrapper pattern for .NET 9 Blazor
   ```

### 📚 Learning

When learning MudBlazor or Blazor concepts:

1. **Ask for explanations**:
   ```
   Explain why MudBlazor doesn't support static SSR rendering
   ```

2. **Request examples**:
   ```
   @mudblazor-dev Show me examples of MudBlazor form validation with data annotations
   ```

3. **Understand patterns**:
   ```
   What's the difference between @rendermode on a page vs on a component?
   ```

## Best Practices for Prompts

### ✅ Good Prompts

**Specific and contextual**:
```
Create a MudDataGrid in Pages/Organizer/Dashboard.razor that displays 
BattleEvent items with columns for Title, Location, EventDate, and JudgeCount.
Include sorting and filtering.
```

**References existing code**:
```
Following the pattern used in Dashboard.razor, create a new page for 
managing registrations with a similar form structure.
```

**States requirements clearly**:
```
Add a MudDialog component for editing categories. It should:
- Use MudTextField for Name (required, max 50 chars)
- Use MudNumericField for MaxAge (range 1-99)
- Include validation and error messages
- Show success notification on save
```

### ❌ Avoid Vague Prompts

**Too generic**:
```
Make a form
```
Better:
```
Create a MudBlazor form for creating battle events with validation
```

**No context**:
```
Fix my component
```
Better:
```
My Dashboard.razor component has a render mode error. The InteractiveWrapper
is showing "ChildContent cannot be serialized". Help me fix it.
```

**Unclear requirements**:
```
Add some features
```
Better:
```
Add edit and delete buttons to the events MudDataGrid with confirmation dialogs
```

## Advanced Features

### Agent Mode in VS Code

Enable Agent Mode for enhanced capabilities:

1. Open Settings (Ctrl+,)
2. Search for "GitHub Copilot"
3. Enable "Agent Mode"

In Agent Mode, Copilot can:
- Make multi-file edits
- Run terminal commands
- Search documentation
- Use MCP tools

### Model Context Protocol (MCP)

The agents can use MCP servers for enhanced capabilities:
- Azure resource access
- Database queries
- External API integration

See [VS Code MCP documentation](https://code.visualstudio.com/docs/copilot/chat/mcp) for setup.

### Custom Instructions

You can add project-specific instructions by editing `copilot-instructions.md`. For example:

```markdown
## Team Conventions

### Naming
- Use PascalCase for component files
- Prefix dialog components with "Dialog"
- Suffix API clients with "ApiClient"

### Structure  
- Keep components under 300 lines
- Extract complex logic to services
- Use dependency injection
```

## Troubleshooting

### Copilot Not Using Instructions

**Symptoms**: Copilot gives generic answers, doesn't follow project patterns

**Solutions**:
1. Verify `.github/copilot-instructions.md` exists
2. Restart VS Code
3. Check Copilot is enabled (status bar icon)
4. Try explicitly referencing: "Following the copilot-instructions.md..."

### Custom Agent Not Available

**Symptoms**: `@mudblazor-dev` not showing in agent selector

**Solutions**:
1. Verify `.github/agents/mudblazor-dev.agent.md` exists
2. Check file has `.agent.md` extension
3. Refresh VS Code
4. Check GitHub Copilot Chat extension is updated

### Outdated Recommendations

**Symptoms**: Copilot suggests old patterns or deprecated code

**Solutions**:
1. Update instructions files with current best practices
2. Explicitly state version: "Using .NET 9 and MudBlazor 8.15..."
3. Reference documentation: "According to the latest MudBlazor docs..."

## Contributing

When you discover new patterns or better practices:

1. **Update copilot-instructions.md** with new patterns
2. **Add to mudblazor-dev.agent.md** for agent-specific knowledge
3. **Create prompts** in `prompts/` for common scenarios
4. **Document examples** of what works well

## Resources

- [GitHub Copilot Documentation](https://docs.github.com/en/copilot)
- [MudBlazor Documentation](https://mudblazor.com)
- [.NET 9 Blazor Render Modes](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes)
- [GitHub Copilot Agent Mode](https://code.visualstudio.com/docs/copilot/chat/chat-agent-mode)

## Feedback

If you have suggestions for improving these configurations:
1. Update the relevant files
2. Document what was changed and why
3. Share with the team

---

**Quick Reference**: 
- General help: Just ask questions naturally
- MudBlazor specific: Use `@mudblazor-dev`
- Templates: Check `prompts/mudblazor-quickstart.md`
- Patterns: See `copilot-instructions.md`
