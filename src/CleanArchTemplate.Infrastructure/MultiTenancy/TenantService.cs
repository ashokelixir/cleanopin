using AutoMapper;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CleanArchTemplate.Infrastructure.MultiTenancy;

/// <summary>
/// Service for tenant management operations
/// </summary>
public class TenantService : ITenantService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<TenantService> _logger;

    public TenantService(
        ITenantRepository tenantRepository,
        IMapper mapper,
        ILogger<TenantService> logger)
    {
        _tenantRepository = tenantRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new tenant
    /// </summary>
    /// <param name="name">The tenant name</param>
    /// <param name="identifier">The tenant identifier</param>
    /// <param name="connectionString">Optional connection string</param>
    /// <param name="configuration">Optional configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created tenant information</returns>
    public async Task<TenantInfo> CreateTenantAsync(string name, string identifier, string? connectionString = null, 
        Dictionary<string, object>? configuration = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new tenant: {Name} ({Identifier})", name, identifier);

        // Check if tenant already exists
        if (await _tenantRepository.ExistsAsync(identifier, cancellationToken))
        {
            throw new InvalidOperationException($"A tenant with identifier '{identifier}' already exists.");
        }

        var configurationJson = configuration != null ? JsonSerializer.Serialize(configuration) : "{}";
        var tenant = Tenant.Create(name, identifier, connectionString, configurationJson);

        var createdTenant = await _tenantRepository.AddAsync(tenant, cancellationToken);
        var tenantInfo = _mapper.Map<TenantInfo>(createdTenant);

        _logger.LogInformation("Tenant created successfully: {TenantId} ({Name})", tenantInfo.Id, tenantInfo.Name);
        return tenantInfo;
    }

    /// <summary>
    /// Gets a tenant by its ID
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant information if found</returns>
    public async Task<TenantInfo?> GetTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        return tenant != null ? _mapper.Map<TenantInfo>(tenant) : null;
    }

    /// <summary>
    /// Gets a tenant by its identifier
    /// </summary>
    /// <param name="identifier">The tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant information if found</returns>
    public async Task<TenantInfo?> GetTenantByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdentifierAsync(identifier, cancellationToken);
        return tenant != null ? _mapper.Map<TenantInfo>(tenant) : null;
    }

    /// <summary>
    /// Gets all tenants
    /// </summary>
    /// <param name="activeOnly">Whether to return only active tenants</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of tenants</returns>
    public async Task<IEnumerable<TenantInfo>> GetAllTenantsAsync(bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var tenants = activeOnly 
            ? await _tenantRepository.GetAllActiveAsync(cancellationToken)
            : await _tenantRepository.GetAllAsync(cancellationToken);

        return _mapper.Map<IEnumerable<TenantInfo>>(tenants);
    }

    /// <summary>
    /// Updates a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="name">The new name</param>
    /// <param name="configuration">The new configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated tenant information</returns>
    public async Task<TenantInfo> UpdateTenantAsync(Guid tenantId, string name, Dictionary<string, object>? configuration = null, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating tenant: {TenantId}", tenantId);

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant with ID '{tenantId}' not found.");
        }

        var configurationJson = configuration != null ? JsonSerializer.Serialize(configuration) : null;
        tenant.Update(name, configurationJson);

        var updatedTenant = await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        var tenantInfo = _mapper.Map<TenantInfo>(updatedTenant);

        _logger.LogInformation("Tenant updated successfully: {TenantId} ({Name})", tenantInfo.Id, tenantInfo.Name);
        return tenantInfo;
    }

    /// <summary>
    /// Activates a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ActivateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Activating tenant: {TenantId}", tenantId);

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant with ID '{tenantId}' not found.");
        }

        tenant.Activate();
        await _tenantRepository.UpdateAsync(tenant, cancellationToken);

        _logger.LogInformation("Tenant activated successfully: {TenantId}", tenantId);
    }

    /// <summary>
    /// Deactivates a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DeactivateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deactivating tenant: {TenantId}", tenantId);

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant with ID '{tenantId}' not found.");
        }

        tenant.Deactivate();
        await _tenantRepository.UpdateAsync(tenant, cancellationToken);

        _logger.LogInformation("Tenant deactivated successfully: {TenantId}", tenantId);
    }

    /// <summary>
    /// Checks if a tenant exists with the given identifier
    /// </summary>
    /// <param name="identifier">The tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the tenant exists</returns>
    public async Task<bool> TenantExistsAsync(string identifier, CancellationToken cancellationToken = default)
    {
        return await _tenantRepository.ExistsAsync(identifier, cancellationToken);
    }

    /// <summary>
    /// Sets the tenant's subscription expiry date
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="expiresAt">The expiry date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task SetSubscriptionExpiryAsync(Guid tenantId, DateTime? expiresAt, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Setting subscription expiry for tenant: {TenantId} to {ExpiresAt}", tenantId, expiresAt);

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant with ID '{tenantId}' not found.");
        }

        tenant.SetSubscriptionExpiry(expiresAt);
        await _tenantRepository.UpdateAsync(tenant, cancellationToken);

        _logger.LogInformation("Subscription expiry set successfully for tenant: {TenantId}", tenantId);
    }
}