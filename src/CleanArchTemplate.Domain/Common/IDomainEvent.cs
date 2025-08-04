using MediatR;

namespace CleanArchTemplate.Domain.Common;

/// <summary>
/// Marker interface for domain events
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// The date and time when the event occurred
    /// </summary>
    DateTime OccurredOn { get; }
}