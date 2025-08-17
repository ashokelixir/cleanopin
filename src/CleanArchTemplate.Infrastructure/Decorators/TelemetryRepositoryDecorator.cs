using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Domain.Common;
using CleanArchTemplate.Domain.Interfaces;
using System.Diagnostics;

namespace CleanArchTemplate.Infrastructure.Decorators;

/// <summary>
/// Decorator for repositories that adds telemetry tracking
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public class TelemetryRepositoryDecorator<T> : IRepository<T> where T : BaseEntity
{
    private readonly IRepository<T> _repository;
    private readonly ITelemetryService _telemetryService;
    private readonly string _entityName;

    public TelemetryRepositoryDecorator(IRepository<T> repository, ITelemetryService telemetryService)
    {
        _repository = repository;
        _telemetryService = telemetryService;
        _entityName = typeof(T).Name;
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        Exception? exception = null;
        T? result = null;

        using var activity = _telemetryService.StartActivity($"Repository.GetById.{_entityName}");
        _telemetryService.AddTag("entity.type", _entityName);
        _telemetryService.AddTag("entity.id", id.ToString());
        _telemetryService.AddTag("operation", "GetById");

        try
        {
            result = await _repository.GetByIdAsync(id, cancellationToken);
            _telemetryService.AddTag("result.found", result != null);
            return result;
        }
        catch (Exception ex)
        {
            exception = ex;
            _telemetryService.RecordException(ex);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _telemetryService.RecordDatabaseOperation(
                $"GetById.{_entityName}",
                stopwatch.Elapsed,
                exception == null,
                _entityName);
        }
    }

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        Exception? exception = null;
        IEnumerable<T> result = Enumerable.Empty<T>();

        using var activity = _telemetryService.StartActivity($"Repository.GetAll.{_entityName}");
        _telemetryService.AddTag("entity.type", _entityName);
        _telemetryService.AddTag("operation", "GetAll");

        try
        {
            result = await _repository.GetAllAsync(cancellationToken);
            var count = result.Count();
            _telemetryService.AddTag("result.count", count);
            return result;
        }
        catch (Exception ex)
        {
            exception = ex;
            _telemetryService.RecordException(ex);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _telemetryService.RecordDatabaseOperation(
                $"GetAll.{_entityName}",
                stopwatch.Elapsed,
                exception == null,
                _entityName);
        }
    }

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        Exception? exception = null;

        using var activity = _telemetryService.StartActivity($"Repository.Add.{_entityName}");
        _telemetryService.AddTag("entity.type", _entityName);
        _telemetryService.AddTag("entity.id", entity.Id.ToString());
        _telemetryService.AddTag("operation", "Add");

        try
        {
            var result = await _repository.AddAsync(entity, cancellationToken);
            _telemetryService.AddTag("result.success", true);
            return result;
        }
        catch (Exception ex)
        {
            exception = ex;
            _telemetryService.RecordException(ex);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _telemetryService.RecordDatabaseOperation(
                $"Add.{_entityName}",
                stopwatch.Elapsed,
                exception == null,
                _entityName);
        }
    }

    public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        Exception? exception = null;

        using var activity = _telemetryService.StartActivity($"Repository.Update.{_entityName}");
        _telemetryService.AddTag("entity.type", _entityName);
        _telemetryService.AddTag("entity.id", entity.Id.ToString());
        _telemetryService.AddTag("operation", "Update");

        try
        {
            await _repository.UpdateAsync(entity, cancellationToken);
            _telemetryService.AddTag("result.success", true);
        }
        catch (Exception ex)
        {
            exception = ex;
            _telemetryService.RecordException(ex);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _telemetryService.RecordDatabaseOperation(
                $"Update.{_entityName}",
                stopwatch.Elapsed,
                exception == null,
                _entityName);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        Exception? exception = null;

        using var activity = _telemetryService.StartActivity($"Repository.Delete.{_entityName}");
        _telemetryService.AddTag("entity.type", _entityName);
        _telemetryService.AddTag("entity.id", id.ToString());
        _telemetryService.AddTag("operation", "Delete");

        try
        {
            await _repository.DeleteAsync(id, cancellationToken);
            _telemetryService.AddTag("result.success", true);
        }
        catch (Exception ex)
        {
            exception = ex;
            _telemetryService.RecordException(ex);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _telemetryService.RecordDatabaseOperation(
                $"Delete.{_entityName}",
                stopwatch.Elapsed,
                exception == null,
                _entityName);
        }
    }

    public IQueryable<T> Query()
    {
        using var activity = _telemetryService.StartActivity($"Repository.Query.{_entityName}");
        _telemetryService.AddTag("entity.type", _entityName);
        _telemetryService.AddTag("operation", "Query");

        return _repository.Query();
    }
}