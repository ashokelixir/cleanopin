using CleanArchTemplate.Application.Common.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CleanArchTemplate.Infrastructure.Services;

/// <summary>
/// Implementation of error logging service with structured logging
/// </summary>
public class ErrorLoggingService : IErrorLoggingService
{
    private readonly ILogger<ErrorLoggingService> _logger;

    public ErrorLoggingService(ILogger<ErrorLoggingService> logger)
    {
        _logger = logger;
    }

    public void LogError(
        Exception exception,
        string correlationId,
        string? userId = null,
        string? requestPath = null,
        string? httpMethod = null,
        Dictionary<string, object>? additionalProperties = null)
    {
        var logProperties = new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["ExceptionType"] = exception.GetType().Name,
            ["ExceptionMessage"] = exception.Message,
            ["StackTrace"] = exception.StackTrace ?? string.Empty
        };

        if (!string.IsNullOrEmpty(userId))
            logProperties["UserId"] = userId;

        if (!string.IsNullOrEmpty(requestPath))
            logProperties["RequestPath"] = requestPath;

        if (!string.IsNullOrEmpty(httpMethod))
            logProperties["HttpMethod"] = httpMethod;

        if (additionalProperties != null)
        {
            foreach (var prop in additionalProperties)
            {
                logProperties[prop.Key] = prop.Value;
            }
        }

        // Add inner exception details if present
        if (exception.InnerException != null)
        {
            logProperties["InnerExceptionType"] = exception.InnerException.GetType().Name;
            logProperties["InnerExceptionMessage"] = exception.InnerException.Message;
        }

        _logger.LogError(exception,
            "Unhandled exception occurred. CorrelationId: {CorrelationId}, ExceptionType: {ExceptionType}, RequestPath: {RequestPath}, Method: {HttpMethod}, UserId: {UserId}",
            correlationId,
            exception.GetType().Name,
            requestPath ?? "Unknown",
            httpMethod ?? "Unknown",
            userId ?? "Anonymous");
    }

    public void LogValidationError(
        IEnumerable<string> validationErrors,
        string correlationId,
        string? userId = null,
        string? requestPath = null,
        string? httpMethod = null,
        object? requestData = null)
    {
        var errors = validationErrors.ToList();
        var logProperties = new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["ValidationErrors"] = errors,
            ["ValidationErrorCount"] = errors.Count
        };

        if (!string.IsNullOrEmpty(userId))
            logProperties["UserId"] = userId;

        if (!string.IsNullOrEmpty(requestPath))
            logProperties["RequestPath"] = requestPath;

        if (!string.IsNullOrEmpty(httpMethod))
            logProperties["HttpMethod"] = httpMethod;

        if (requestData != null)
        {
            try
            {
                // Serialize request data but exclude sensitive information
                var sanitizedData = SanitizeRequestData(requestData);
                logProperties["RequestData"] = JsonSerializer.Serialize(sanitizedData);
            }
            catch (Exception ex)
            {
                logProperties["RequestDataSerializationError"] = ex.Message;
            }
        }

        _logger.LogWarning(
            "Validation errors occurred. CorrelationId: {CorrelationId}, ErrorCount: {ValidationErrorCount}, RequestPath: {RequestPath}, Method: {HttpMethod}, UserId: {UserId}, Errors: {ValidationErrors}",
            correlationId,
            errors.Count,
            requestPath ?? "Unknown",
            httpMethod ?? "Unknown",
            userId ?? "Anonymous",
            string.Join("; ", errors));
    }

    public void LogAuthorizationFailure(
        string userId,
        string requiredPermission,
        string resource,
        string correlationId,
        string? requestPath = null,
        string? httpMethod = null)
    {
        _logger.LogWarning(
            "Authorization failed. CorrelationId: {CorrelationId}, UserId: {UserId}, RequiredPermission: {RequiredPermission}, Resource: {Resource}, RequestPath: {RequestPath}, Method: {HttpMethod}",
            correlationId,
            userId,
            requiredPermission,
            resource,
            requestPath ?? "Unknown",
            httpMethod ?? "Unknown");
    }

    public void LogAuthenticationFailure(
        string reason,
        string correlationId,
        string? requestPath = null,
        string? httpMethod = null,
        Dictionary<string, object>? attemptedCredentials = null)
    {
        var logProperties = new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["AuthenticationFailureReason"] = reason
        };

        if (!string.IsNullOrEmpty(requestPath))
            logProperties["RequestPath"] = requestPath;

        if (!string.IsNullOrEmpty(httpMethod))
            logProperties["HttpMethod"] = httpMethod;

        if (attemptedCredentials != null)
        {
            // Only log non-sensitive credential information
            var sanitizedCredentials = attemptedCredentials
                .Where(kvp => !IsSensitiveField(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            
            if (sanitizedCredentials.Any())
                logProperties["AttemptedCredentials"] = sanitizedCredentials;
        }

        _logger.LogWarning(
            "Authentication failed. CorrelationId: {CorrelationId}, Reason: {AuthenticationFailureReason}, RequestPath: {RequestPath}, Method: {HttpMethod}",
            correlationId,
            reason,
            requestPath ?? "Unknown",
            httpMethod ?? "Unknown");
    }

    private static object SanitizeRequestData(object requestData)
    {
        if (requestData == null)
            return new { };

        var json = JsonSerializer.Serialize(requestData);
        var dictionary = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        if (dictionary == null)
            return new { };

        var sanitized = new Dictionary<string, object>();

        foreach (var kvp in dictionary)
        {
            if (IsSensitiveField(kvp.Key))
            {
                sanitized[kvp.Key] = "***REDACTED***";
            }
            else
            {
                sanitized[kvp.Key] = kvp.Value.ToString() ?? string.Empty;
            }
        }

        return sanitized;
    }

    private static bool IsSensitiveField(string fieldName)
    {
        var sensitiveFields = new[]
        {
            "password", "pwd", "secret", "token", "key", "credential",
            "authorization", "auth", "ssn", "socialsecuritynumber",
            "creditcard", "cardnumber", "cvv", "pin", "otp"
        };

        return sensitiveFields.Any(field => 
            fieldName.Contains(field, StringComparison.OrdinalIgnoreCase));
    }
}