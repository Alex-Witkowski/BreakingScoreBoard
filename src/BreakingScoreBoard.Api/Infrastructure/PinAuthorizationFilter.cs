using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BreakingScoreBoard.Api.Infrastructure;

/// <summary>
/// Authorization filter that validates PIN from X-Pin header.
/// </summary>
public class PinAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly PinAuthService _pinAuthService;
    private readonly bool _requireAdmin;

    public PinAuthorizationFilter(PinAuthService pinAuthService, bool requireAdmin = false)
    {
        _pinAuthService = pinAuthService;
        _requireAdmin = requireAdmin;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var pin = context.HttpContext.Request.Headers["X-Pin"].FirstOrDefault();

        if (string.IsNullOrEmpty(pin))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Missing X-Pin header" });
            return;
        }

        // Check for global admin PIN first
        if (_pinAuthService.ValidateGlobalAdminPin(pin))
        {
            return;
        }

        // Try to get event ID from route
        if (!context.RouteData.Values.TryGetValue("eventId", out var eventIdObj) ||
            !Guid.TryParse(eventIdObj?.ToString(), out var eventId))
        {
            // If no event ID in route, try from query or body
            if (context.HttpContext.Request.Query.TryGetValue("eventId", out var queryEventId) &&
                Guid.TryParse(queryEventId.FirstOrDefault(), out eventId))
            {
                // Found in query
            }
            else
            {
                context.Result = new UnauthorizedObjectResult(new { error = "Invalid PIN" });
                return;
            }
        }

        bool isValid;
        if (_requireAdmin)
        {
            isValid = await _pinAuthService.ValidateEventAdminPinAsync(eventId, pin);
        }
        else
        {
            isValid = await _pinAuthService.ValidateEventPinAsync(eventId, pin);
        }

        if (!isValid)
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Invalid PIN" });
        }
    }
}

/// <summary>
/// Attribute to require PIN authentication for admin operations.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireAdminPinAttribute : TypeFilterAttribute
{
    public RequireAdminPinAttribute() : base(typeof(PinAuthorizationFilter))
    {
        Arguments = new object[] { true };
    }
}

/// <summary>
/// Attribute to require PIN authentication (admin or judge).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequirePinAttribute : TypeFilterAttribute
{
    public RequirePinAttribute() : base(typeof(PinAuthorizationFilter))
    {
        Arguments = new object[] { false };
    }
}

/// <summary>
/// Attribute factory for creating PinAuthorizationFilter instances.
/// </summary>
public class PinAuthorizationFilterFactory : IFilterFactory
{
    private readonly bool _requireAdmin;

    public PinAuthorizationFilterFactory(bool requireAdmin = false)
    {
        _requireAdmin = requireAdmin;
    }

    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var pinAuthService = serviceProvider.GetRequiredService<PinAuthService>();
        return new PinAuthorizationFilter(pinAuthService, _requireAdmin);
    }
}
