using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Domain.Common;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Infrastructure.Data.Contexts;
using CleanArchTemplate.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Infrastructure.Data.Repositories;

/// <summary>
/// Base repository with resilience patterns for database operations
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public abstract class ResilientBaseRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext Context;
    protected readonly DbSet<T> DbSet;
    protected readonly ILogger Logger;
    private readonly IResilienceService _resilienceService;

    protected ResilientBaseRepository(
        ApplicationDbContext context, 
        ILogger logger,
        IResilienceService resilienceService)
    {
        Context = context;
        DbSet = context.Set<T>();
        Logger = logger;
        _resilienceService = resilienceService;
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _resilienceService.ExecuteAsync(
            async () =>
            {
                Logger.LogDebug("Getting entity {EntityType} with ID: {Id}", typeof(T).Name, id);
                return await DbSet.FindAsync(id, cancellationToken);
            },
            ApplicationConstants.ResiliencePolicies.Database);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _resilienceService.ExecuteAsync(
            async () =>
            {
                Logger.LogDebug("Getting all entities of type {EntityType}", typeof(T).Name);
                return await DbSet.ToListAsync(cancellationToken);
            },
            ApplicationConstants.ResiliencePolicies.Database);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        return await _resilienceService.ExecuteAsync(
            async () =>
            {
                Logger.LogDebug("Adding entity {EntityType} with ID: {Id}", typeof(T).Name, entity.Id);
                var result = await DbSet.AddAsync(entity, cancellationToken);
                return result.Entity;
            },
            ApplicationConstants.ResiliencePolicies.Database);
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _resilienceService.ExecuteAsync(
            async () =>
            {
                Logger.LogDebug("Updating entity {EntityType} with ID: {Id}", typeof(T).Name, entity.Id);
                DbSet.Update(entity);
                await Task.CompletedTask;
            },
            ApplicationConstants.ResiliencePolicies.Database);
    }

    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _resilienceService.ExecuteAsync(
            async () =>
            {
                Logger.LogDebug("Deleting entity {EntityType} with ID: {Id}", typeof(T).Name, id);
                var entity = await DbSet.FindAsync(id, cancellationToken);
                if (entity != null)
                {
                    DbSet.Remove(entity);
                }
            },
            ApplicationConstants.ResiliencePolicies.Database);
    }

    public virtual IQueryable<T> Query()
    {
        return _resilienceService.Execute(
            () =>
            {
                Logger.LogDebug("Creating query for entity type {EntityType}", typeof(T).Name);
                return DbSet.AsQueryable();
            },
            ApplicationConstants.ResiliencePolicies.Database);
    }

    /// <summary>
    /// Executes a database operation with resilience patterns and fallback
    /// </summary>
    /// <typeparam name="TResult">Result type</typeparam>
    /// <param name="operation">Primary database operation</param>
    /// <param name="fallback">Fallback operation if primary fails</param>
    /// <returns>Result of operation or fallback</returns>
    protected async Task<TResult> ExecuteWithFallbackAsync<TResult>(
        Func<Task<TResult>> operation, 
        Func<Task<TResult>> fallback)
    {
        return await _resilienceService.ExecuteWithFallbackAsync(
            operation, 
            fallback, 
            ApplicationConstants.ResiliencePolicies.Database);
    }

    /// <summary>
    /// Executes a critical database operation with enhanced resilience
    /// </summary>
    /// <typeparam name="TResult">Result type</typeparam>
    /// <param name="operation">Critical database operation</param>
    /// <returns>Result of operation</returns>
    protected async Task<TResult> ExecuteCriticalAsync<TResult>(Func<Task<TResult>> operation)
    {
        return await _resilienceService.ExecuteAsync(
            operation, 
            ApplicationConstants.ResiliencePolicies.Critical);
    }
}