using CleanArchTemplate.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Diagnostics;

namespace CleanArchTemplate.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly IAuditLogService _auditLogService;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        IAuditLogService auditLogService)
    {
        _logger = logger;
        _auditLogService = auditLogService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        using (LogContext.PushProperty("RequestType", requestName))
        using (LogContext.PushProperty("RequestData", request, destructureObjects: true))
        {
            _logger.LogInformation("Starting request: {RequestName}", requestName);

            try
            {
                var response = await next();
                
                stopwatch.Stop();
                var elapsedMs = stopwatch.ElapsedMilliseconds;

                using (LogContext.PushProperty("ElapsedMs", elapsedMs))
                using (LogContext.PushProperty("Success", true))
                {
                    var logLevel = elapsedMs > 5000 ? LogLevel.Warning : LogLevel.Information;
                    _logger.Log(logLevel, "Completed request: {RequestName} in {ElapsedMs}ms", requestName, elapsedMs);
                }

                // Log business events for audit purposes
                await LogBusinessEventIfNeeded(requestName, request, response);
                
                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var elapsedMs = stopwatch.ElapsedMilliseconds;

                using (LogContext.PushProperty("ElapsedMs", elapsedMs))
                using (LogContext.PushProperty("Success", false))
                using (LogContext.PushProperty("ErrorType", ex.GetType().Name))
                {
                    _logger.LogError(ex, "Request failed: {RequestName} in {ElapsedMs}ms", requestName, elapsedMs);
                }

                throw;
            }
        }
    }

    private async Task LogBusinessEventIfNeeded(string requestName, TRequest request, TResponse response)
    {
        // Log audit events for specific command types
        if (IsAuditableCommand(requestName))
        {
            var eventType = GetEventTypeFromRequest(requestName);
            var description = $"Executed {requestName}";
            var data = new Dictionary<string, object>
            {
                ["RequestType"] = requestName,
                ["RequestData"] = request?.ToString() ?? "null",
                ["Success"] = true
            };

            await _auditLogService.LogSecurityEventAsync(eventType, description, additionalData: data);
        }
    }

    private static bool IsAuditableCommand(string requestName)
    {
        var auditableCommands = new[]
        {
            "CreateUserCommand",
            "UpdateUserCommand",
            "DeleteUserCommand",
            "CreateRoleCommand",
            "UpdateRoleCommand",
            "DeleteRoleCommand",
            "AssignRoleCommand",
            "RemoveRoleCommand",
            "LoginCommand",
            "RefreshTokenCommand"
        };

        return auditableCommands.Any(cmd => requestName.Contains(cmd, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetEventTypeFromRequest(string requestName)
    {
        return requestName switch
        {
            var name when name.Contains("Login", StringComparison.OrdinalIgnoreCase) => "Authentication",
            var name when name.Contains("User", StringComparison.OrdinalIgnoreCase) => "UserManagement",
            var name when name.Contains("Role", StringComparison.OrdinalIgnoreCase) => "RoleManagement",
            var name when name.Contains("Permission", StringComparison.OrdinalIgnoreCase) => "PermissionManagement",
            _ => "BusinessOperation"
        };
    }
}