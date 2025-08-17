using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Infrastructure.Data.Interceptors;

/// <summary>
/// EF Core interceptor that dispatches domain events after SaveChanges
/// </summary>
public class DomainEventDispatchingInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatchingInterceptor> _logger;

    public DomainEventDispatchingInterceptor(
        IServiceProvider serviceProvider,
        ILogger<DomainEventDispatchingInterceptor> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (eventData.Context is not null)
        {
            DispatchDomainEventsAsync(eventData.Context).GetAwaiter().GetResult();
        }

        return base.SavedChanges(eventData, result);
    }

    private async Task DispatchDomainEventsAsync(DbContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get entities with domain events
            var entitiesWithEvents = context.ChangeTracker.Entries<BaseEntity>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();

            if (!entitiesWithEvents.Any())
                return;

            _logger.LogDebug("Found {EntityCount} entities with domain events", entitiesWithEvents.Count);

            // Get the domain event dispatcher from the service provider
            using var scope = _serviceProvider.CreateScope();
            var dispatcher = scope.ServiceProvider.GetService<IDomainEventDispatcher>();
            
            if (dispatcher != null)
            {
                // Dispatch all domain events
                await dispatcher.DispatchEventsAsync(entitiesWithEvents, cancellationToken);
                _logger.LogDebug("Successfully dispatched domain events for {EntityCount} entities", entitiesWithEvents.Count);
            }
            else
            {
                _logger.LogWarning("IDomainEventDispatcher service not found, domain events will not be dispatched");
            }

            // Clear the events after dispatching
            foreach (var entity in entitiesWithEvents)
            {
                entity.ClearDomainEvents();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while dispatching domain events");
            
            // Clear the events even if dispatching failed to prevent them from being dispatched again
            var entitiesWithEvents = context.ChangeTracker.Entries<BaseEntity>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();

            foreach (var entity in entitiesWithEvents)
            {
                entity.ClearDomainEvents();
            }
        }
    }
}