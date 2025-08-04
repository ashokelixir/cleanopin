using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace CleanArchTemplate.Infrastructure.Data.Interceptors;

public class PerformanceLoggingInterceptor : DbCommandInterceptor
{
    private readonly ILogger<PerformanceLoggingInterceptor> _logger;
    private readonly TimeSpan _slowQueryThreshold;

    public PerformanceLoggingInterceptor(ILogger<PerformanceLoggingInterceptor> logger)
    {
        _logger = logger;
        _slowQueryThreshold = TimeSpan.FromMilliseconds(500); // Log queries taking more than 500ms
    }

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        await LogCommandPerformance(command, eventData);
        return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        LogCommandPerformance(command, eventData).GetAwaiter().GetResult();
        return base.ReaderExecuted(command, eventData, result);
    }

    public override async ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        await LogCommandPerformance(command, eventData);
        return await base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override object? ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result)
    {
        LogCommandPerformance(command, eventData).GetAwaiter().GetResult();
        return base.ScalarExecuted(command, eventData, result);
    }

    public override async ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await LogCommandPerformance(command, eventData);
        return await base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        LogCommandPerformance(command, eventData).GetAwaiter().GetResult();
        return base.NonQueryExecuted(command, eventData, result);
    }

    public override async Task CommandFailedAsync(
        DbCommand command,
        CommandErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _logger.LogError(eventData.Exception,
            "Database command failed after {Duration}ms. Command: {CommandText}",
            eventData.Duration.TotalMilliseconds,
            command.CommandText);

        await base.CommandFailedAsync(command, eventData, cancellationToken);
    }

    public override void CommandFailed(
        DbCommand command,
        CommandErrorEventData eventData)
    {
        _logger.LogError(eventData.Exception,
            "Database command failed after {Duration}ms. Command: {CommandText}",
            eventData.Duration.TotalMilliseconds,
            command.CommandText);

        base.CommandFailed(command, eventData);
    }

    private async Task LogCommandPerformance(DbCommand command, CommandExecutedEventData eventData)
    {
        var duration = eventData.Duration;
        var commandText = command.CommandText;
        var parameters = GetParameterValues(command);

        if (duration > _slowQueryThreshold)
        {
            _logger.LogWarning(
                "Slow database query detected. Duration: {Duration}ms, Command: {CommandText}, Parameters: {@Parameters}",
                duration.TotalMilliseconds,
                commandText,
                parameters);
        }
        else
        {
            _logger.LogDebug(
                "Database query executed. Duration: {Duration}ms, Command: {CommandText}, Parameters: {@Parameters}",
                duration.TotalMilliseconds,
                commandText,
                parameters);
        }

        await Task.CompletedTask;
    }

    private static Dictionary<string, object?> GetParameterValues(DbCommand command)
    {
        var parameters = new Dictionary<string, object?>();
        
        foreach (DbParameter parameter in command.Parameters)
        {
            parameters[parameter.ParameterName] = parameter.Value;
        }

        return parameters;
    }
}