# Project Structure

## Solution Organization

The solution follows Clean Architecture principles with clear separation of concerns across layers:

```
CleanArchTemplate.sln
├── src/                           # Source code
│   ├── CleanArchTemplate.API/     # Presentation layer
│   ├── CleanArchTemplate.Application/ # Application layer
│   ├── CleanArchTemplate.Domain/  # Domain layer (core)
│   ├── CleanArchTemplate.Infrastructure/ # Infrastructure layer
│   └── CleanArchTemplate.Shared/  # Shared utilities
├── tests/                         # Test projects
│   ├── CleanArchTemplate.UnitTests/
│   ├── CleanArchTemplate.IntegrationTests/
│   ├── CleanArchTemplate.ArchitectureTests/
│   └── CleanArchTemplate.TestUtilities/
├── scripts/                       # Build and deployment scripts
├── terraform/                     # Infrastructure as Code
├── docs/                         # Documentation
└── examples/                     # Code examples
```

## Layer Dependencies

**Dependency Flow** (inner layers should not depend on outer layers):
- Domain ← Application ← Infrastructure
- Domain ← Application ← API
- All layers can reference Shared

## Domain Layer (`CleanArchTemplate.Domain`)

**Core business logic with no external dependencies**

```
Domain/
├── Entities/          # Domain entities (User, Role, Permission)
├── ValueObjects/      # Value objects (Email, etc.)
├── Enums/            # Domain enums (PermissionState)
├── Events/           # Domain events
├── Interfaces/       # Repository and service contracts
├── Services/         # Domain services
├── Common/           # Base classes and shared domain logic
└── Exceptions/       # Domain-specific exceptions
```

**Key Patterns:**
- Rich domain models with behavior
- Domain events for cross-cutting concerns
- Value objects for primitive obsession
- Repository interfaces (implementation in Infrastructure)

## Application Layer (`CleanArchTemplate.Application`)

**Use cases and application logic**

```
Application/
├── Features/         # Feature-based organization (CQRS)
│   ├── Users/
│   │   ├── Commands/ # User commands (Create, Update, Delete)
│   │   └── Queries/  # User queries (GetById, GetAll)
│   └── Roles/
├── Services/         # Application services
├── Common/           # Shared application logic
├── Behaviors/        # MediatR pipeline behaviors
├── Mappings/         # AutoMapper profiles
└── DTOs/            # Data transfer objects
```

**Key Patterns:**
- CQRS with MediatR
- Feature folders organization
- Command/Query handlers
- Validation with FluentValidation
- Pipeline behaviors for cross-cutting concerns

## Infrastructure Layer (`CleanArchTemplate.Infrastructure`)

**External concerns and implementations**

```
Infrastructure/
├── Data/
│   ├── Configurations/  # EF Core entity configurations
│   ├── Repositories/    # Repository implementations
│   ├── Migrations/      # EF Core migrations
│   └── Seed/           # Database seeding
├── Services/           # External service implementations
├── Configuration/      # Configuration providers
├── Caching/           # Redis caching implementation
└── Messaging/         # Message queue implementations
```

**Key Patterns:**
- Repository pattern implementation
- EF Core with configuration classes
- Service implementations for external APIs
- Caching abstractions

## API Layer (`CleanArchTemplate.API`)

**Web API controllers and configuration**

```
API/
├── Controllers/       # API controllers
├── Middleware/        # Custom middleware
├── Filters/          # Action filters
├── Extensions/       # Service registration extensions
└── Configuration/    # Startup configuration
```

**Key Patterns:**
- Minimal controllers (delegate to MediatR)
- API versioning
- Swagger/OpenAPI documentation
- JWT authentication
- Global exception handling

## Test Projects

### Unit Tests (`CleanArchTemplate.UnitTests`)
- Test domain logic and application handlers
- Mock external dependencies
- Fast execution, no I/O operations

### Integration Tests (`CleanArchTemplate.IntegrationTests`)
- Test API endpoints end-to-end
- Use test database and containers
- Verify complete request/response flow

### Architecture Tests (`CleanArchTemplate.ArchitectureTests`)
- Enforce architectural rules
- Verify layer dependencies
- Ensure naming conventions

## Naming Conventions

### Files and Folders
- **PascalCase** for all C# files and folders
- **Feature-based** organization in Application layer
- **Plural** names for entity collections (Users/, Roles/)

### Classes and Methods
- **Entities**: PascalCase nouns (User, Role, Permission)
- **Commands**: Verb + Entity + Command (CreateUserCommand)
- **Queries**: Get + Entity + Query (GetUserByIdQuery)
- **Handlers**: Command/Query + Handler (CreateUserCommandHandler)
- **Repositories**: I + Entity + Repository (IUserRepository)

### Database
- **Tables**: PascalCase singular (User, Role, Permission)
- **Columns**: PascalCase (FirstName, LastName, CreatedAt)
- **Foreign Keys**: EntityId (UserId, RoleId)

## Configuration Files

- **appsettings.json**: Base configuration
- **appsettings.Development.json**: Development overrides
- **Directory.Build.props**: Solution-wide MSBuild properties
- **global.json**: .NET SDK version
- **docker-compose.yml**: Development environment
- **.editorconfig**: Code formatting rules