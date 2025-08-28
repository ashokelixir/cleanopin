using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Infrastructure.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Infrastructure.MultiTenancy.Resolvers;

/// <summary>
/// Resolves tenant information from HTTP headers
/// </summary>
public class HeaderTenantResolver : IHttpTenantResolver
{
    private readonly ITenantService _tenantService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HeaderTenantResolver> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    // Header names to check for tenant information
    private static readonly string[] TenantHeaders = { "X-Tenant-ID", "X-Tenant-Identifier", "Tenant-ID" };

    public int Priority => 2; // Medium priority
    public string Name => "Header";

    public HeaderTenantResolver(
        ITenantService tenantService,
        IMemoryCache cache,
        ILogger<HeaderTenantResolver> logger)
    {
        _tenantService = tenantService;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Resolves tenant information from HTTP headers
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant information if found</returns>
    public async Task<TenantInfo?> ResolveTenantAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        foreach (var headerName in TenantHeaders)
        {
            if (context.Request.Headers.TryGetValue(headerName, out var headerValues))
            {
                var headerValue = headerValues.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(headerValue))
                {
                    _logger.LogDebug("Found tenant identifier in header {HeaderName}: {Value}", headerName, headerValue);
                    
                    // Try to parse as GUID first (tenant ID)
                    if (Guid.TryParse(headerValue, out var tenantId))
                    {
                        return await ResolveTenantByIdAsync(tenantId, cancellationToken);
                    }
                    
                    // Otherwise treat as identifier
                    return await ResolveTenantAsync(headerValue, cancellationToken);
                }
            }
        }

        _logger.LogDebug("No tenant headers found in request");
        return null;
    }

    /// <summary>
    /// Resolves tenant information from an identifier
    /// </summary>
    /// <param name="identifier">The tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant information if found</returns>
    public async Task<TenantInfo?> ResolveTenantAsync(string identifier, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return null;

        var cacheKey = $"tenant:header:{identifier.ToLowerInvariant()}";

        // Try to get from cache first
        if (_cache.TryGetValue(cacheKey, out TenantInfo? cachedTenant))
        {
            _logger.LogDebug("Tenant resolved from cache for identifier: {Identifier}", identifier);
            return cachedTenant;
        }

        try
        {
            var tenant = await _tenantService.GetTenantByIdentifierAsync(identifier, cancellationToken);
            
            if (tenant != null)
            {
                // Cache the result
                _cache.Set(cacheKey, tenant, CacheDuration);
                _logger.LogDebug("Tenant resolved and cached for identifier: {Identifier}, TenantId: {TenantId}", 
                    identifier, tenant.Id);
            }
            else
            {
                _logger.LogDebug("No tenant found for identifier: {Identifier}", identifier);
            }

            return tenant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving tenant for identifier: {Identifier}", identifier);
            return null;
        }
    }

    /// <summary>
    /// Resolves tenant information by tenant ID
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant information if found</returns>
    private async Task<TenantInfo?> ResolveTenantByIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"tenant:id:{tenantId}";

        // Try to get from cache first
        if (_cache.TryGetValue(cacheKey, out TenantInfo? cachedTenant))
        {
            _logger.LogDebug("Tenant resolved from cache for ID: {TenantId}", tenantId);
            return cachedTenant;
        }

        try
        {
            var tenant = await _tenantService.GetTenantAsync(tenantId, cancellationToken);
            
            if (tenant != null)
            {
                // Cache the result
                _cache.Set(cacheKey, tenant, CacheDuration);
                _logger.LogDebug("Tenant resolved and cached for ID: {TenantId}", tenantId);
            }
            else
            {
                _logger.LogDebug("No tenant found for ID: {TenantId}", tenantId);
            }

            return tenant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving tenant for ID: {TenantId}", tenantId);
            return null;
        }
    }
}