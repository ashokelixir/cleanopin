using CleanArchTemplate.Domain.Common;

namespace CleanArchTemplate.Application.Common.Interfaces;

/// <summary>
/// Service for dispatching domain events
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches all domain events from the given entities
    /// </summary>
    /// <param name="entities">Entities with domain events</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task DispatchEventsAsync(IEnumerable<BaseEntity> entities, CancellationToken cancellationToken = default);
}