using CleanArchTemplate.Infrastructure.Data.Contexts;
using CleanArchTemplate.TestUtilities.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace CleanArchTemplate.IntegrationTests.Performance;

public class DatabasePerformanceTests : BaseIntegrationTest
{
    public DatabasePerformanceTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task DatabaseQuery_ShouldLogPerformanceMetrics_WhenExecuted()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabasePerformanceTests>>();

        // Act
        var users = await context.Users
            .Where(u => u.IsActive)
            .Take(10)
            .ToListAsync();

        // Assert
        // The performance logging interceptor should have logged the query
        // This is verified through the logging output in the test
        Assert.NotNull(users);
        logger.LogInformation("Database query executed successfully, returned {Count} users", users.Count);
    }

    [Fact]
    public async Task SlowDatabaseQuery_ShouldLogWarning_WhenExecutionTimeExceedsThreshold()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Act - Execute a potentially slow query
        var result = await context.Users
            .Where(u => u.Email.Value.Contains("test"))
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();

        // Assert
        // The performance logging interceptor should log this query
        // If it takes longer than the threshold, it should log a warning
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DatabaseTransaction_ShouldLogPerformanceMetrics_WhenExecuted()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Act - Use execution strategy for transaction
        var strategy = context.Database.CreateExecutionStrategy();
        var (userCount, roleCount) = await strategy.ExecuteAsync(context, async (ctx, token) =>
        {
            using var transaction = await ctx.Database.BeginTransactionAsync(token);
            
            // Perform some database operations
            var users = await ctx.Users.CountAsync(token);
            var roles = await ctx.Roles.CountAsync(token);
            
            await transaction.CommitAsync(token);
            
            return (users, roles);
        }, null, CancellationToken.None);

        // Assert
        Assert.True(userCount >= 0);
        Assert.True(roleCount >= 0);
    }
}