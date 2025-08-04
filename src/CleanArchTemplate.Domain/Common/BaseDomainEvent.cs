namespace CleanArchTemplate.Domain.Common;

/// <summary>
/// Base class for domain events
/// </summary>
public abstract class BaseDomainEvent : IDomainEvent
{
    /// <summary>
    /// The date and time when the event occurred
    /// </summary>
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}