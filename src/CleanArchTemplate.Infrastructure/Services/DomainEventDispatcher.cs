using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Infrastructure.Services;

/// <summary>
/// Service for dispatching domain events using MediatR
/// </summary>
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(IMediator mediator, ILogger<DomainEventDispatcher> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task DispatchEventsAsync(IEnumerable<BaseEntity> entities, CancellationToken cancellationToken = default)
    {
        var domainEvents = entities
            .SelectMany(x => x.DomainEvents)
            .ToList();

        if (!domainEvents.Any())
            return;

        _logger.LogDebug("Dispatching {EventCount} domain events", domainEvents.Count);

        foreach (var domainEvent in domainEvents)
        {
            try
            {
                _logger.LogDebug("Dispatching domain event: {EventType}", domainEvent.GetType().Name);
                await _mediator.Publish(domainEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispatching domain event: {EventType}", domainEvent.GetType().Name);
                // Continue with other events even if one fails
            }
        }

        _logger.LogDebug("Completed dispatching {EventCount} domain events", domainEvents.Count);
    }
}