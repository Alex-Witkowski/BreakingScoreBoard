using System.Diagnostics;

namespace BreakingScoreBoard.Api.Infrastructure;

/// <summary>
/// Middleware to add correlation IDs to requests for distributed tracing and logging purposes.
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private const string TraceIdHeader = "X-Trace-ID";
    private readonly RequestDelegate _next = next;
    private readonly ILogger<CorrelationIdMiddleware> _logger = logger;

    /// <summary>
    /// Processes the HTTP request and response.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Get or create correlation ID
        var correlationId = context.Request.Headers.TryGetValue(CorrelationIdHeader, out var existingId)
            ? existingId.ToString()
            : Guid.NewGuid().ToString("N");

        // Get trace ID from Activity if available
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        // Store correlation ID in HttpContext.Items for access in services
        context.Items[CorrelationIdHeader] = correlationId;
        context.Items[TraceIdHeader] = traceId;

        // Add correlation and trace IDs to response headers
        context.Response.Headers[CorrelationIdHeader] = correlationId;
        context.Response.Headers[TraceIdHeader] = traceId;

        // Log incoming request with correlation ID
        _logger.LogInformation(
            "Incoming HTTP {Method} {Path} | CorrelationId: {CorrelationId} | TraceId: {TraceId}",
            context.Request.Method,
            context.Request.Path,
            correlationId,
            traceId);

        try
        {
            await _next(context);

            _logger.LogInformation(
                "Completed HTTP {Method} {Path} with status {StatusCode} | CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing HTTP {Method} {Path} | CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                correlationId);
            throw;
        }
    }
}

/// <summary>
/// Service to retrieve the correlation ID for the current HTTP context.
/// </summary>
public class CorrelationIdAccessor(IHttpContextAccessor httpContextAccessor)
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private const string TraceIdHeader = "X-Trace-ID";
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    /// <summary>
    /// Gets the correlation ID for the current request.
    /// </summary>
    public string? CorrelationId =>
        _httpContextAccessor.HttpContext?.Items[CorrelationIdHeader]?.ToString();

    /// <summary>
    /// Gets the trace ID for the current request.
    /// </summary>
    public string? TraceId =>
        _httpContextAccessor.HttpContext?.Items[TraceIdHeader]?.ToString();
}
