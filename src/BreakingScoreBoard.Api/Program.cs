using System.Reflection;
using BreakingScoreBoard.Api.Contracts;
using BreakingScoreBoard.Api.Infrastructure;
using BreakingScoreBoard.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddDbContext<BattleDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add PIN authentication service
builder.Services.AddScoped<PinAuthService>();

// Add domain services
builder.Services.AddScoped<ScoringService>();
builder.Services.AddScoped<PreSelectionService>();
builder.Services.AddScoped<BracketService>();

// Add HTTP context accessor for correlation IDs
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CorrelationIdAccessor>();

// Add HttpClient for Blazor components to call API
builder.Services.AddHttpClient();

// Add Blazor Server
builder.Services.AddRazorPages();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add controllers
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BreakingScoreBoard API",
        Version = "v1",
        Description = "API for managing breaking (breakdance) battles with age categories, pre-selection rounds, knockout brackets, and judge scoring."
    });

    // Add X-Pin header parameter
    options.AddSecurityDefinition("Pin", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-Pin",
        Description = "PIN for authentication (admin or judge PIN)"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Pin"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline

// Add correlation ID middleware first
app.UseMiddleware<CorrelationIdMiddleware>();

// Global exception handler
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var error = ErrorResponse.FromMessage("An unexpected error occurred");
        await context.Response.WriteAsJsonAsync(error);
    });
});

// Enable Swagger in all environments for API documentation
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "BreakingScoreBoard API v1");
    options.RoutePrefix = "swagger";
});

app.UseRouting();
app.UseAntiforgery();

// Map Blazor
app.MapRazorPages();
app.MapRazorComponents<BreakingScoreBoard.Api.Components.App>()
    .AddInteractiveServerRenderMode();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
