using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.ValueObjects;
using CleanArchTemplate.Infrastructure.Data;
using CleanArchTemplate.Infrastructure.Data.Contexts;
using CleanArchTemplate.TestUtilities.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CleanArchTemplate.UnitTests.Infrastructure.Data;

/// <summary>
/// Tests for UnitOfWork transaction handling with execution strategies
/// </summary>
public class UnitOfWorkTransactionTests : BaseTest
{
    [Fact]
    public async Task ExecuteInTransactionAsync_WithValidOperation_ShouldCommitSuccessfully()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var unitOfWork = new UnitOfWork(context);

        var email = Email.Create("test@example.com");
        var user = User.Create(email, "Test", "User", "hashedPassword");

        // Act
        var result = await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await unitOfWork.Users.AddAsync(user);
            return user;
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        
        // Verify user was saved
        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.NotNull(savedUser);
        Assert.Equal("test@example.com", savedUser.Email.Value);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithException_ShouldRollbackTransaction()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var unitOfWork = new UnitOfWork(context);

        var email = Email.Create("test2@example.com");
        var user = User.Create(email, "Test", "User", "hashedPassword");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await unitOfWork.Users.AddAsync(user);
                throw new InvalidOperationException("Test exception");
            });
        });

        // Verify user was not saved due to rollback
        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.Null(savedUser);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_VoidOperation_ShouldCommitSuccessfully()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var unitOfWork = new UnitOfWork(context);

        var email = Email.Create("test3@example.com");
        var user = User.Create(email, "Test", "User", "hashedPassword");

        // Act
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await unitOfWork.Users.AddAsync(user);
        });

        // Assert
        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.NotNull(savedUser);
        Assert.Equal("test3@example.com", savedUser.Email.Value);
    }
}