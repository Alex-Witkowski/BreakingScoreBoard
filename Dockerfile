# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY BreakingScoreBoard.sln ./
COPY src/BreakingScoreBoard.Api/BreakingScoreBoard.Api.csproj src/BreakingScoreBoard.Api/
COPY src/BreakingScoreBoard.Domain/BreakingScoreBoard.Domain.csproj src/BreakingScoreBoard.Domain/
COPY src/BreakingScoreBoard.Tests/BreakingScoreBoard.Tests.csproj src/BreakingScoreBoard.Tests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ src/

# Build and publish the application
WORKDIR /src/src/BreakingScoreBoard.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published application
COPY --from=build /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "BreakingScoreBoard.Api.dll"]
