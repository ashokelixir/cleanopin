using CleanArchTemplate.API.Models;
using CleanArchTemplate.Application.Common.Exceptions;
using CleanArchTemplate.Domain.Exceptions;
using FluentValidation;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Security;
using System.Text.Json;

namespace CleanArchTemplate.API.Middleware;

/// <summary>
/// Global exception handling middleware that catches and processes all unhandled exceptions
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next, 
        ILogger<GlobalExceptionMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? 
                           context.TraceIdentifier;

        _logger.LogError(exception, 
            "An unhandled exception occurred. CorrelationId: {CorrelationId}, RequestPath: {RequestPath}, Method: {Method}",
            correlationId, context.Request.Path, context.Request.Method);

        var response = CreateErrorResponse(exception, correlationId);
        
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = response.StatusCode;

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private ErrorResponse CreateErrorResponse(Exception exception, string correlationId)
    {
        return exception switch
        {
            DomainValidationException domainValidationEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.UnprocessableEntity,
                Title = "Validation Error",
                Detail = domainValidationEx.Message,
                Instance = correlationId,
                Errors = domainValidationEx.Errors.ToList(),
                Type = "https://tools.ietf.org/html/rfc4918#section-11.2"
            },
            
            EntityNotFoundException entityNotFoundEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                Title = "Resource Not Found",
                Detail = entityNotFoundEx.Message,
                Instance = correlationId,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"
            },
            
            BusinessRuleViolationException businessRuleEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Title = "Business Rule Violation",
                Detail = businessRuleEx.Message,
                Instance = correlationId,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Extensions = new Dictionary<string, object> { ["rule"] = businessRuleEx.Rule }
            },
            
            PermissionNotFoundException permissionNotFoundEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                Title = "Permission Not Found",
                Detail = permissionNotFoundEx.Message,
                Instance = correlationId,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"
            },
            
            ApplicationValidationException appValidationEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.UnprocessableEntity,
                Title = "Application Validation Error",
                Detail = appValidationEx.Message,
                Instance = correlationId,
                Errors = appValidationEx.Errors.ToList(),
                Type = "https://tools.ietf.org/html/rfc4918#section-11.2"
            },
            
            ValidationException fluentValidationEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.UnprocessableEntity,
                Title = "Validation Error",
                Detail = "One or more validation errors occurred.",
                Instance = correlationId,
                Errors = fluentValidationEx.Errors.Select(e => e.ErrorMessage).ToList(),
                Type = "https://tools.ietf.org/html/rfc4918#section-11.2"
            },
            
            ConflictException conflictEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.Conflict,
                Title = "Resource Conflict",
                Detail = conflictEx.Message,
                Instance = correlationId,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                Extensions = new Dictionary<string, object> 
                { 
                    ["resource"] = conflictEx.Resource,
                    ["conflictingValue"] = conflictEx.ConflictingValue.ToString() ?? string.Empty
                }
            },
            
            ExternalServiceException externalServiceEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadGateway,
                Title = "External Service Error",
                Detail = externalServiceEx.Message,
                Instance = correlationId,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.3",
                Extensions = new Dictionary<string, object> 
                { 
                    ["serviceName"] = externalServiceEx.ServiceName,
                    ["operation"] = externalServiceEx.Operation
                }
            },
            
            AuthenticationException authEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Title = "Authentication Failed",
                Detail = authEx.Message,
                Instance = correlationId,
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                Extensions = new Dictionary<string, object> { ["reason"] = authEx.Reason }
            },
            
            AuthorizationException authzEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.Forbidden,
                Title = "Authorization Failed",
                Detail = authzEx.Message,
                Instance = correlationId,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                Extensions = new Dictionary<string, object> 
                { 
                    ["requiredPermission"] = authzEx.RequiredPermission,
                    ["resource"] = authzEx.Resource
                }
            },
            
            UnauthorizedAccessException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Title = "Unauthorized",
                Detail = "Authentication is required to access this resource.",
                Instance = correlationId,
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
            },
            
            SecurityException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.Forbidden,
                Title = "Forbidden",
                Detail = "You do not have permission to access this resource.",
                Instance = correlationId,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3"
            },
            
            InvalidOperationException invalidOpEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Title = "Invalid Operation",
                Detail = invalidOpEx.Message,
                Instance = correlationId,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            },
            
            ArgumentException argumentEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Title = "Invalid Argument",
                Detail = argumentEx.Message,
                Instance = correlationId,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            },
            
            TimeoutException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.RequestTimeout,
                Title = "Request Timeout",
                Detail = "The request timed out. Please try again later.",
                Instance = correlationId,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.7"
            },
            
            TaskCanceledException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.RequestTimeout,
                Title = "Request Cancelled",
                Detail = "The request was cancelled or timed out.",
                Instance = correlationId,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.7"
            },
            
            DomainException domainEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Title = "Domain Error",
                Detail = domainEx.Message,
                Instance = correlationId,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            },
            
            _ => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Title = "Internal Server Error",
                Detail = _environment.IsDevelopment() 
                    ? exception.Message 
                    : "An unexpected error occurred. Please try again later.",
                Instance = correlationId,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Extensions = _environment.IsDevelopment() 
                    ? new Dictionary<string, object> 
                    { 
                        ["stackTrace"] = exception.StackTrace ?? string.Empty,
                        ["exceptionType"] = exception.GetType().Name
                    }
                    : null
            }
        };
    }
}

/// <summary>
/// Extension methods for registering the global exception middleware
/// </summary>
public static class GlobalExceptionMiddlewareExtensions
{
    /// <summary>
    /// Adds the global exception middleware to the pipeline
    /// </summary>
    /// <param name="builder">The application builder</param>
    /// <returns>The application builder</returns>
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionMiddleware>();
    }
}