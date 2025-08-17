using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Shared.Constants;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Infrastructure.Data;

/// <summary>
/// Unit of Work wrapper with resilience patterns for database operations
/// </summary>
public class ResilientUnitOfWork : IDisposable
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IResilienceService _resilienceService;
    private readonly ILogger<ResilientUnitOfWork> _logger;

    public ResilientUnitOfWork(
        IUnitOfWork unitOfWork,
        IResilienceService resilienceService,
        ILogger<ResilientUnitOfWork> logger)
    {
        _unitOfWork = unitOfWork;
        _resilienceService = resilienceService;
        _logger = logger;
    }

    public IUserRepository Users => _unitOfWork.Users;
    public IRoleRepository Roles => _unitOfWork.Roles;
    public IPermissionRepository Permissions => _unitOfWork.Permissions;
    public IRefreshTokenRepository RefreshTokens => _unitOfWork.RefreshTokens;

    /// <summary>
    /// Saves changes with resilience patterns
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of affected records</returns>
    public async Task<int> SaveChangesWithResilienceAsync(CancellationToken cancellationToken = default)
    {
        return await _resilienceService.ExecuteAsync(
            async () =>
            {
                _logger.LogDebug("Saving changes with resilience");
                return await _unitOfWork.SaveChangesAsync(cancellationToken);
            },
            ApplicationConstants.ResiliencePolicies.Database);
    }

    /// <summary>
    /// Begins transaction with resilience patterns
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    public async Task BeginTransactionWithResilienceAsync(CancellationToken cancellationToken = default)
    {
        await _resilienceService.ExecuteAsync(
            async () =>
            {
                _logger.LogDebug("Beginning transaction with resilience");
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
            },
            ApplicationConstants.ResiliencePolicies.Database);
    }

    /// <summary>
    /// Commits transaction with resilience patterns
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    public async Task CommitTransactionWithResilienceAsync(CancellationToken cancellationToken = default)
    {
        await _resilienceService.ExecuteAsync(
            async () =>
            {
                _logger.LogDebug("Committing transaction with resilience");
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            },
            ApplicationConstants.ResiliencePolicies.Critical);
    }

    /// <summary>
    /// Rolls back transaction with resilience patterns
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    public async Task RollbackTransactionWithResilienceAsync(CancellationToken cancellationToken = default)
    {
        await _resilienceService.ExecuteAsync(
            async () =>
            {
                _logger.LogDebug("Rolling back transaction with resilience");
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            },
            ApplicationConstants.ResiliencePolicies.Database);
    }

    /// <summary>
    /// Executes a database operation within a transaction with resilience patterns
    /// Uses EF Core's execution strategy for proper retry handling with transactions
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="operation">Database operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation, 
        CancellationToken cancellationToken = default)
    {
        return await _resilienceService.ExecuteAsync(
            async () =>
            {
                _logger.LogDebug("Executing operation in transaction with resilience using execution strategy");
                return await _unitOfWork.ExecuteInTransactionAsync(operation, cancellationToken);
            },
            ApplicationConstants.ResiliencePolicies.Critical);
    }

    /// <summary>
    /// Executes a database operation within a transaction with resilience patterns
    /// Uses EF Core's execution strategy for proper retry handling with transactions
    /// </summary>
    /// <param name="operation">Database operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    public async Task ExecuteInTransactionAsync(
        Func<Task> operation, 
        CancellationToken cancellationToken = default)
    {
        await _resilienceService.ExecuteAsync(
            async () =>
            {
                _logger.LogDebug("Executing void operation in transaction with resilience using execution strategy");
                await _unitOfWork.ExecuteInTransactionAsync(operation, cancellationToken);
            },
            ApplicationConstants.ResiliencePolicies.Critical);
    }

    /// <summary>
    /// Executes a database operation with fallback mechanism
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="operation">Primary database operation</param>
    /// <param name="fallback">Fallback operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result or fallback result</returns>
    public async Task<T> ExecuteWithFallbackAsync<T>(
        Func<Task<T>> operation,
        Func<Task<T>> fallback,
        CancellationToken cancellationToken = default)
    {
        return await _resilienceService.ExecuteWithFallbackAsync(
            operation,
            fallback,
            ApplicationConstants.ResiliencePolicies.Database);
    }

    public void Dispose()
    {
        _unitOfWork?.Dispose();
    }
}