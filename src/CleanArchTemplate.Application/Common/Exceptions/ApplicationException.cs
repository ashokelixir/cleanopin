namespace CleanArchTemplate.Application.Common.Exceptions;

/// <summary>
/// Base exception for application layer errors
/// </summary>
public abstract class ApplicationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the ApplicationException class
    /// </summary>
    /// <param name="message">The error message</param>
    protected ApplicationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the ApplicationException class
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="innerException">The inner exception</param>
    protected ApplicationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a validation error occurs in the application layer
/// </summary>
public sealed class ApplicationValidationException : ApplicationException
{
    /// <summary>
    /// The validation errors
    /// </summary>
    public IReadOnlyCollection<string> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the ApplicationValidationException class
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="errors">The validation errors</param>
    public ApplicationValidationException(string message, IEnumerable<string> errors) : base(message)
    {
        Errors = errors.ToList().AsReadOnly();
    }

    /// <summary>
    /// Initializes a new instance of the ApplicationValidationException class
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="error">A single validation error</param>
    public ApplicationValidationException(string message, string error) : base(message)
    {
        Errors = new List<string> { error }.AsReadOnly();
    }
}

/// <summary>
/// Exception thrown when a resource conflict occurs
/// </summary>
public sealed class ConflictException : ApplicationException
{
    /// <summary>
    /// The resource that caused the conflict
    /// </summary>
    public string Resource { get; }

    /// <summary>
    /// The conflicting value
    /// </summary>
    public object ConflictingValue { get; }

    /// <summary>
    /// Initializes a new instance of the ConflictException class
    /// </summary>
    /// <param name="resource">The resource that caused the conflict</param>
    /// <param name="conflictingValue">The conflicting value</param>
    /// <param name="message">The error message</param>
    public ConflictException(string resource, object conflictingValue, string message) : base(message)
    {
        Resource = resource;
        ConflictingValue = conflictingValue;
    }
}

/// <summary>
/// Exception thrown when an external service is unavailable
/// </summary>
public sealed class ExternalServiceException : ApplicationException
{
    /// <summary>
    /// The name of the external service
    /// </summary>
    public string ServiceName { get; }

    /// <summary>
    /// The operation that failed
    /// </summary>
    public string Operation { get; }

    /// <summary>
    /// Initializes a new instance of the ExternalServiceException class
    /// </summary>
    /// <param name="serviceName">The name of the external service</param>
    /// <param name="operation">The operation that failed</param>
    /// <param name="message">The error message</param>
    public ExternalServiceException(string serviceName, string operation, string message) : base(message)
    {
        ServiceName = serviceName;
        Operation = operation;
    }

    /// <summary>
    /// Initializes a new instance of the ExternalServiceException class
    /// </summary>
    /// <param name="serviceName">The name of the external service</param>
    /// <param name="operation">The operation that failed</param>
    /// <param name="message">The error message</param>
    /// <param name="innerException">The inner exception</param>
    public ExternalServiceException(string serviceName, string operation, string message, Exception innerException) 
        : base(message, innerException)
    {
        ServiceName = serviceName;
        Operation = operation;
    }
}

/// <summary>
/// Exception thrown when authentication fails
/// </summary>
public sealed class AuthenticationException : ApplicationException
{
    /// <summary>
    /// The reason for authentication failure
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Initializes a new instance of the AuthenticationException class
    /// </summary>
    /// <param name="reason">The reason for authentication failure</param>
    /// <param name="message">The error message</param>
    public AuthenticationException(string reason, string message) : base(message)
    {
        Reason = reason;
    }
}

/// <summary>
/// Exception thrown when authorization fails
/// </summary>
public sealed class AuthorizationException : ApplicationException
{
    /// <summary>
    /// The required permission
    /// </summary>
    public string RequiredPermission { get; }

    /// <summary>
    /// The resource being accessed
    /// </summary>
    public string Resource { get; }

    /// <summary>
    /// Initializes a new instance of the AuthorizationException class
    /// </summary>
    /// <param name="requiredPermission">The required permission</param>
    /// <param name="resource">The resource being accessed</param>
    /// <param name="message">The error message</param>
    public AuthorizationException(string requiredPermission, string resource, string message) : base(message)
    {
        RequiredPermission = requiredPermission;
        Resource = resource;
    }
}