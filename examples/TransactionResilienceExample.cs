using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Examples;

/// <summary>
/// Example demonstrating transaction resilience patterns with Entity Framework Core
/// This shows the correct way to handle transactions with retry policies
/// </summary>
public class TransactionResilienceExample
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IResilienceService _resilienceService;
    private readonly ILogger<TransactionResilienceExample> _logger;

    public TransactionResilienceExample(
        IUnitOfWork unitOfWork,
        IResilienceService resilienceService,
        ILogger<TransactionResilienceExample> logger)
    {
        _unitOfWork = unitOfWork;
        _resilienceService = resilienceService;
        _logger = logger;
    }

    /// <summary>
    /// Example 1: Simple transaction using EF Core's execution strategy
    /// This is the recommended approach for most scenarios
    /// </summary>
    public async Task SimpleTransactionExample()
    {
        _logger.LogInformation("=== Simple Transaction Example ===");

        var email = Email.Create("simple@example.com");
        var user = User.Create(email, "Simple", "User", "hashedPassword");

        try
        {
            // Use the UnitOfWork's ExecuteInTransactionAsync method
            // This handles the execution strategy and transaction lifecycle automatically
            var result = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _unitOfWork.Users.AddAsync(user);
                _logger.LogInformation("User added to context within transaction");
                return user;
            });

            _logger.LogInformation("✅ Transaction completed successfully. User ID: {UserId}", result.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Transaction failed");
            throw;
        }
    }

    /// <summary>
    /// Example 2: Transaction with additional resilience layer
    /// This combines EF Core's execution strategy with Polly policies
    /// </summary>
    public async Task ResilientTransactionExample()
    {
        _logger.LogInformation("=== Resilient Transaction Example ===");

        var email = Email.Create("resilient@example.com");
        var user = User.Create(email, "Resilient", "User", "hashedPassword");

        try
        {
            // Wrap the transaction in an additional resilience layer
            var result = await _resilienceService.ExecuteAsync(
                async () => await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Check if user already exists
                    var exists = await _unitOfWork.Users.IsEmailExistsAsync(email, null);
                    if (exists)
                    {
                        throw new InvalidOperationException("User already exists");
                    }

                    await _unitOfWork.Users.AddAsync(user);
                    _logger.LogInformation("User added with additional resilience checks");
                    return user;
                }),
                "Critical"); // Use critical resilience policy

            _logger.LogInformation("✅ Resilient transaction completed successfully. User ID: {UserId}", result.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Resilient transaction failed");
            throw;
        }
    }

    /// <summary>
    /// Example 3: Multiple operations in a single transaction
    /// Shows how to perform multiple database operations atomically
    /// </summary>
    public async Task MultipleOperationsTransactionExample()
    {
        _logger.LogInformation("=== Multiple Operations Transaction Example ===");

        var userEmail = Email.Create("multi@example.com");
        var user = User.Create(userEmail, "Multi", "User", "hashedPassword");

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Add user
                await _unitOfWork.Users.AddAsync(user);
                _logger.LogInformation("User added to transaction");

                // Create a role for the user (if roles exist)
                var adminRole = await _unitOfWork.Roles.GetByNameAsync("Admin");
                if (adminRole != null)
                {
                    // Add user to admin role
                    var userRole = UserRole.Create(user.Id, adminRole.Id);
                    // Note: This would require a UserRole repository method
                    _logger.LogInformation("User role assignment prepared");
                }

                _logger.LogInformation("Multiple operations prepared for commit");
            });

            _logger.LogInformation("✅ Multiple operations transaction completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Multiple operations transaction failed");
            throw;
        }
    }

    /// <summary>
    /// Example 4: Transaction with compensation logic
    /// Shows how to handle partial failures and cleanup
    /// </summary>
    public async Task TransactionWithCompensationExample()
    {
        _logger.LogInformation("=== Transaction with Compensation Example ===");

        var email = Email.Create("compensation@example.com");
        var user = User.Create(email, "Compensation", "User", "hashedPassword");

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _unitOfWork.Users.AddAsync(user);
                _logger.LogInformation("User added to transaction");

                // Simulate a condition that might require compensation
                var existingUsersCount = await _unitOfWork.Users.CountAsync();
                if (existingUsersCount > 100) // Business rule example
                {
                    _logger.LogWarning("Too many users, transaction will be rolled back");
                    throw new InvalidOperationException("User limit exceeded");
                }

                _logger.LogInformation("Business rules validated, proceeding with commit");
            });

            _logger.LogInformation("✅ Transaction with compensation completed successfully");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("User limit"))
        {
            _logger.LogWarning("Transaction rolled back due to business rule: {Message}", ex.Message);
            // Compensation logic could go here (e.g., notify admin, log metrics)
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Transaction with compensation failed unexpectedly");
            throw;
        }
    }

    /// <summary>
    /// Example 5: What NOT to do - Manual transaction management with retry policies
    /// This will cause the InvalidOperationException we're fixing
    /// </summary>
    public async Task IncorrectTransactionExample()
    {
        _logger.LogInformation("=== Incorrect Transaction Example (Will Fail) ===");

        var email = Email.Create("incorrect@example.com");
        var user = User.Create(email, "Incorrect", "User", "hashedPassword");

        try
        {
            // ❌ DON'T DO THIS - Manual transaction with retry policies enabled
            await _unitOfWork.BeginTransactionAsync();
            
            try
            {
                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync(); // This will throw InvalidOperationException
                await _unitOfWork.CommitTransactionAsync();
                
                _logger.LogInformation("This won't be reached due to exception");
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("execution strategy"))
        {
            _logger.LogError("❌ Expected error: {Message}", ex.Message);
            _logger.LogInformation("This demonstrates why manual transactions don't work with retry policies");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unexpected error in incorrect transaction example");
        }
    }

    /// <summary>
    /// Runs all transaction examples
    /// </summary>
    public async Task RunAllExamples()
    {
        _logger.LogInformation("Starting Transaction Resilience Examples...\n");

        try
        {
            await SimpleTransactionExample();
            await Task.Delay(1000); // Brief pause between examples

            await ResilientTransactionExample();
            await Task.Delay(1000);

            await MultipleOperationsTransactionExample();
            await Task.Delay(1000);

            await TransactionWithCompensationExample();
            await Task.Delay(1000);

            // Uncomment to see the error that we fixed
            // await IncorrectTransactionExample();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Example execution failed");
        }

        _logger.LogInformation("\n=== All Transaction Examples Completed ===");
    }
}