# CleanArchTemplate.Application

This is the Application layer of the Clean Architecture template, implementing the CQRS pattern using MediatR.

## Structure

### Common
- **Interfaces**: Repository and service interfaces
- **Models**: DTOs and data transfer objects
- **Behaviors**: MediatR pipeline behaviors for cross-cutting concerns
- **Mappings**: AutoMapper profiles for entity-to-DTO mapping

### Features
Organized by domain features, each containing:
- **Commands**: Write operations (Create, Update, Delete)
- **Queries**: Read operations (Get, List)
- **Validators**: FluentValidation validators for each command/query

## Key Components

### CQRS Implementation
- **Commands**: Handle write operations and business logic
- **Queries**: Handle read operations and data retrieval
- **Handlers**: Process commands and queries using MediatR

### Validation
- **FluentValidation**: Input validation for all commands and queries
- **ValidationBehavior**: Pipeline behavior that runs validation before handlers

### Logging
- **LoggingBehavior**: Pipeline behavior that logs request/response information

### Mapping
- **AutoMapper**: Maps between domain entities and DTOs
- **MappingProfile**: Centralized mapping configuration

## Dependencies

- **MediatR**: CQRS implementation
- **AutoMapper**: Object-to-object mapping
- **FluentValidation**: Input validation
- **Microsoft.EntityFrameworkCore**: For query operations

## Usage

Register the application services in your DI container:

```csharp
services.AddApplication();
```

This will register:
- MediatR with pipeline behaviors
- AutoMapper with profiles
- FluentValidation validators

## Examples

### Command Usage
```csharp
var command = new CreateUserCommand("user@example.com", "John", "Doe", "password123");
var result = await mediator.Send(command);
```

### Query Usage
```csharp
var query = new GetUserByIdQuery(userId);
var user = await mediator.Send(query);
```

### Paginated Query Usage
```csharp
var query = new GetAllUsersQuery(new PaginationRequest { PageNumber = 1, PageSize = 10 });
var paginatedUsers = await mediator.Send(query);
```