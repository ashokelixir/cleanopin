# Transaction Resilience with Entity Framework Core

## Problem

When using Entity Framework Core with PostgreSQL and enabling retry policies via `EnableRetryOnFailure`, you cannot use user-initiated transactions (explicit `BeginTransaction` calls) because the retrying execution strategy doesn't support them.

The error you'll encounter:
```
System.InvalidOperationException: The configured execution strategy 'NpgsqlRetryingExecutionStrategy' does not support user-initiated transactions. Use the execution strategy returned by 'DbContext.Database.CreateExecutionStrategy()' to execute all the operations in the transaction as a retriable unit.
```

## Solution

Use Entity Framework Core's execution strategy pattern to handle transactions with retry logic. The execution strategy manages the entire transaction lifecycle, including retries on transient failures.

### Implementation

#### 1. Updated UnitOfWork Pattern

```csharp
public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
{
    var strategy = _context.Database.CreateExecutionStrategy();
    return await strategy.ExecuteAsync(async () =>
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation();
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    });
}
```

#### 2. Usage in Command Handlers

**Before (Problematic):**
```csharp
await _unitOfWork.BeginTransactionAsync(cancellationToken);
try
{
    await _unitOfWork.Users.AddAsync(user, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);
    await _unitOfWork.CommitTransactionAsync(cancellationToken);
}
catch
{
    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
    throw;
}
```

**After (Fixed):**
```csharp
await _unitOfWork.ExecuteInTransactionAsync(async () =>
{
    await _unitOfWork.Users.AddAsync(user, cancellationToken);
}, cancellationToken);
```

## Best Practices

### 1. Keep Transactions Short
- Minimize the work done within transactions
- Avoid external API calls within transactions
- Move audit logging outside of transactions when possible

### 2. Idempotent Operations
- Ensure operations can be safely retried
- Use unique constraints to prevent duplicate data
- Handle partial failures gracefully

### 3. Resilience Layering
- Use execution strategies for database-level resilience
- Use Polly policies for application-level resilience
- Don't double-wrap resilience patterns unnecessarily

### 4. Error Handling
- Let transient exceptions bubble up to the execution strategy
- Handle business logic exceptions separately
- Log retry attempts for monitoring

## Configuration

The retry behavior is configured in `DependencyInjection.cs`:

```csharp
options.UseNpgsql(connectionString, npgsqlOptions =>
{
    npgsqlOptions.EnableRetryOnFailure(
        maxRetryCount: 3,
        maxRetryDelay: TimeSpan.FromSeconds(5),
        errorCodesToAdd: null);
});
```

## Monitoring

- Monitor retry attempts through EF Core logging
- Track transaction rollbacks and failures
- Set up alerts for excessive retry patterns

## Testing

When testing code that uses execution strategies:
- Use in-memory databases for unit tests (they don't support execution strategies)
- Use real databases for integration tests to verify retry behavior
- Test transient failure scenarios explicitly