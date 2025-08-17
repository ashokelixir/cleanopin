namespace CleanArchTemplate.Application.Common.Services;

/// <summary>
/// Service for structured error logging with correlation
/// </summary>
public interface IErrorLoggingService
{
    /// <summary>
    /// Logs an error with correlation information
    /// </summary>
    /// <param name="exception">The exception to log</param>
    /// <param name="correlationId">The correlation ID</param>
    /// <param name="userId">The user ID (if available)</param>
    /// <param name="requestPath">The request path</param>
    /// <param name="httpMethod">The HTTP method</param>
    /// <param name="additionalProperties">Additional properties to log</param>
    void LogError(
        Exception exception,
        string correlationId,
        string? userId = null,
        string? requestPath = null,
        string? httpMethod = null,
        Dictionary<string, object>? additionalProperties = null);

    /// <summary>
    /// Logs a validation error with correlation information
    /// </summary>
    /// <param name="validationErrors">The validation errors</param>
    /// <param name="correlationId">The correlation ID</param>
    /// <param name="userId">The user ID (if available)</param>
    /// <param name="requestPath">The request path</param>
    /// <param name="httpMethod">The HTTP method</param>
    /// <param name="requestData">The request data that failed validation</param>
    void LogValidationError(
        IEnumerable<string> validationErrors,
        string correlationId,
        string? userId = null,
        string? requestPath = null,
        string? httpMethod = null,
        object? requestData = null);

    /// <summary>
    /// Logs an authorization failure with correlation information
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="requiredPermission">The required permission</param>
    /// <param name="resource">The resource being accessed</param>
    /// <param name="correlationId">The correlation ID</param>
    /// <param name="requestPath">The request path</param>
    /// <param name="httpMethod">The HTTP method</param>
    void LogAuthorizationFailure(
        string userId,
        string requiredPermission,
        string resource,
        string correlationId,
        string? requestPath = null,
        string? httpMethod = null);

    /// <summary>
    /// Logs an authentication failure with correlation information
    /// </summary>
    /// <param name="reason">The reason for authentication failure</param>
    /// <param name="correlationId">The correlation ID</param>
    /// <param name="requestPath">The request path</param>
    /// <param name="httpMethod">The HTTP method</param>
    /// <param name="attemptedCredentials">Information about the attempted credentials (sanitized)</param>
    void LogAuthenticationFailure(
        string reason,
        string correlationId,
        string? requestPath = null,
        string? httpMethod = null,
        Dictionary<string, object>? attemptedCredentials = null);
}