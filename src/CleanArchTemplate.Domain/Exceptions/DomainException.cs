namespace CleanArchTemplate.Domain.Exceptions;

/// <summary>
/// Base exception for domain-related errors
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the DomainException class
    /// </summary>
    /// <param name="message">The error message</param>
    protected DomainException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the DomainException class
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="innerException">The inner exception</param>
    protected DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a domain validation fails
/// </summary>
public sealed class DomainValidationException : DomainException
{
    /// <summary>
    /// The validation errors
    /// </summary>
    public IReadOnlyCollection<string> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the DomainValidationException class
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="errors">The validation errors</param>
    public DomainValidationException(string message, IEnumerable<string> errors) : base(message)
    {
        Errors = errors.ToList().AsReadOnly();
    }

    /// <summary>
    /// Initializes a new instance of the DomainValidationException class
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="error">A single validation error</param>
    public DomainValidationException(string message, string error) : base(message)
    {
        Errors = new List<string> { error }.AsReadOnly();
    }
}

/// <summary>
/// Exception thrown when a domain entity is not found
/// </summary>
public sealed class EntityNotFoundException : DomainException
{
    /// <summary>
    /// The entity type that was not found
    /// </summary>
    public string EntityType { get; }

    /// <summary>
    /// The entity identifier that was not found
    /// </summary>
    public object EntityId { get; }

    /// <summary>
    /// Initializes a new instance of the EntityNotFoundException class
    /// </summary>
    /// <param name="entityType">The entity type</param>
    /// <param name="entityId">The entity identifier</param>
    public EntityNotFoundException(string entityType, object entityId) 
        : base($"{entityType} with ID '{entityId}' was not found.")
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}

/// <summary>
/// Exception thrown when a domain business rule is violated
/// </summary>
public sealed class BusinessRuleViolationException : DomainException
{
    /// <summary>
    /// The business rule that was violated
    /// </summary>
    public string Rule { get; }

    /// <summary>
    /// Initializes a new instance of the BusinessRuleViolationException class
    /// </summary>
    /// <param name="rule">The business rule</param>
    /// <param name="message">The error message</param>
    public BusinessRuleViolationException(string rule, string message) : base(message)
    {
        Rule = rule;
    }
}