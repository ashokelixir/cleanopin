# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy project files for dependency resolution
COPY ["src/CleanArchTemplate.API/CleanArchTemplate.API.csproj", "src/CleanArchTemplate.API/"]
COPY ["src/CleanArchTemplate.Application/CleanArchTemplate.Application.csproj", "src/CleanArchTemplate.Application/"]
COPY ["src/CleanArchTemplate.Domain/CleanArchTemplate.Domain.csproj", "src/CleanArchTemplate.Domain/"]
COPY ["src/CleanArchTemplate.Infrastructure/CleanArchTemplate.Infrastructure.csproj", "src/CleanArchTemplate.Infrastructure/"]
COPY ["src/CleanArchTemplate.Shared/CleanArchTemplate.Shared.csproj", "src/CleanArchTemplate.Shared/"]
COPY ["Directory.Build.props", "./"]
COPY ["global.json", "./"]

# Restore dependencies
RUN dotnet restore "src/CleanArchTemplate.API/CleanArchTemplate.API.csproj"

# Copy source code
COPY src/ src/

# Build the application
WORKDIR "/src/src/CleanArchTemplate.API"
RUN dotnet build "CleanArchTemplate.API.csproj" -c Release --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish "CleanArchTemplate.API.csproj" -c Release -o /app/publish \
    --no-restore --no-build \
    /p:UseAppHost=false \
    /p:PublishTrimmed=false \
    /p:PublishSingleFile=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final

# Install security updates and required packages including ICU for globalization
RUN apk update && apk upgrade && \
    apk add --no-cache \
    ca-certificates \
    tzdata \
    icu-libs && \
    rm -rf /var/cache/apk/*

# Create non-root user
RUN addgroup -g 1001 -S appgroup && \
    adduser -u 1001 -S appuser -G appgroup

# Set working directory
WORKDIR /app

# Copy published application
COPY --from=publish --chown=appuser:appgroup /app/publish .

# Create directories with proper permissions
RUN mkdir -p /app/logs /app/temp && \
    chown -R appuser:appgroup /app

# Switch to non-root user
USER appuser

# Configure environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

# Entry point with proper signal handling
ENTRYPOINT ["dotnet", "CleanArchTemplate.API.dll"]