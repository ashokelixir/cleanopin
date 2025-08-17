# Technology Stack

## Framework & Runtime
- **.NET 9.0**: Latest LTS version with C# 12 features
- **ASP.NET Core**: Web API framework with OpenAPI/Swagger support
- **Entity Framework Core 9.0**: ORM with PostgreSQL provider

## Architecture Patterns
- **Clean Architecture**: Domain-centric design with clear layer separation
- **CQRS**: Command Query Responsibility Segregation with MediatR
- **Domain-Driven Design**: Rich domain models with domain events
- **Repository Pattern**: Data access abstraction

## Key Libraries & Frameworks
- **MediatR**: CQRS and mediator pattern implementation
- **FluentValidation**: Input validation
- **AutoMapper**: Object-to-object mapping
- **Serilog**: Structured logging with multiple sinks
- **JWT Bearer**: Authentication and authorization
- **Polly**: Resilience patterns (retry, circuit breaker, timeout)
- **xUnit**: Unit testing framework
- **Moq**: Mocking framework
- **FluentAssertions**: Fluent test assertions

## Infrastructure & Services
- **PostgreSQL**: Primary database
- **Redis**: Caching and session storage
- **AWS Services**: Secrets Manager, SQS, S3
- **LocalStack**: Local AWS services for development
- **Docker**: Containerization and development environment
- **Seq**: Centralized logging and monitoring

## Build & Development Commands

### Building
```powershell
# Build entire solution
dotnet build /property:WarningLevel=0

# Build specific project
dotnet build src/CleanArchTemplate.API /property:WarningLevel=0

# Build for release
dotnet build -c Release /property:WarningLevel=0
```

### Testing
```powershell
# Run all tests
./scripts/run-tests.ps1

# Run with coverage
./scripts/run-tests.ps1 -Coverage

# Run specific test types
./scripts/run-tests.ps1 -UnitOnly
./scripts/run-tests.ps1 -IntegrationOnly
./scripts/run-tests.ps1 -ArchitectureOnly

# Run with filter
./scripts/run-tests.ps1 -Filter "FullyQualifiedName~UserService"
```

### Docker Development
```powershell
# Start development environment
./scripts/docker-dev.ps1 up

# Stop environment
./scripts/docker-dev.ps1 down

# View logs
./scripts/docker-dev.ps1 logs

# Clean up resources
./scripts/docker-dev.ps1 clean
```

### Database Migrations
```powershell
# Add new migration
dotnet ef migrations add MigrationName -p src/CleanArchTemplate.Infrastructure -s src/CleanArchTemplate.API

# Update database
dotnet ef database update -p src/CleanArchTemplate.Infrastructure -s src/CleanArchTemplate.API
```

## Code Quality & Standards
- **Nullable Reference Types**: Enabled across all projects
- **Treat Warnings as Errors**: Enforced for code quality
- **Code Analysis**: .NET analyzers enabled with latest rules
- **Documentation**: XML documentation required for public APIs
- **EditorConfig**: Consistent code formatting rules