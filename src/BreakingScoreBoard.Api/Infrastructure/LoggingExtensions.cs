namespace BreakingScoreBoard.Api.Infrastructure; public static class LoggingExtensions
{
    public static void LogBusinessOperation(this ILogger logger, string? correlationId, string operation, string entityType, object entityId)
    {
        logger.LogInformation(
            "Business Operation: {Operation} {EntityType} {EntityId} | CorrelationId: {CorrelationId}",
            operation,
            entityType,
            entityId,
            correlationId);
    }
}
