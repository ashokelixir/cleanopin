using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Infrastructure.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace CleanArchTemplate.Infrastructure.MultiTenancy.Resolvers;

/// <summary>
/// Resolves tenant information from JWT claims
/// </summary>
public class JwtTenantResolver : IHttpTenantResolver
{
    private readonly ITenantService _tenantService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<JwtTenantResolver> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    // Claim names to check for tenant information
    private static readonly string[] TenantClaims = { "tenant_id", "tenantId", "tenant", "tid" };

    public int Priority => 3; // Lower priority (used when user is authenticated)
    public string Name => "JWT";

    public JwtTenantResolver(
        ITenantService tenantService,
        IMemoryCache cache,
        ILogger<JwtTenantResolver> logger)
    {
        _tenantService = tenantService;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Resolves tenant information from JWT claims in the HTTP context
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant information if found</returns>
    public async Task<TenantInfo?> ResolveTenantAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        var user = context.User;
        
        if (user?.Identity?.IsAuthenticated != true)
        {
            _logger.LogDebug("User is not authenticated, cannot resolve tenant from JWT");
            return null;
        }

        foreach (var claimName in TenantClaims)
        {
            var tenantClaim = user.FindFirst(claimName);
            if (tenantClaim != null && !string.IsNullOrWhiteSpace(tenantClaim.Value))
            {
                _logger.LogDebug("Found tenant identifier in JWT claim {ClaimName}: {Value}", claimName, tenantClaim.Value);
                
                // Try to parse as GUID first (tenant ID)
                if (Guid.TryParse(tenantClaim.Value, out var tenantId))
                {
                    return await ResolveTenantByIdAsync(tenantId, cancellationToken);
                }
                
                // Otherwise treat as identifier
                return await ResolveTenantAsync(tenantClaim.Value, cancellationToken);
            }
        }

        _logger.LogDebug("No tenant claims found in JWT");
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

        var cacheKey = $"tenant:jwt:{identifier.ToLowerInvariant()}";

        // Try to get from cache first
        if (_cache.TryGetValue(cacheKey, out TenantInfo? cachedTenant))
        {
            _logger.LogDebug("Tenant resolved from cache for JWT identifier: {Identifier}", identifier);
            return cachedTenant;
        }

        try
        {
            var tenant = await _tenantService.GetTenantByIdentifierAsync(identifier, cancellationToken);
            
            if (tenant != null)
            {
                // Cache the result
                _cache.Set(cacheKey, tenant, CacheDuration);
                _logger.LogDebug("Tenant resolved and cached for JWT identifier: {Identifier}, TenantId: {TenantId}", 
                    identifier, tenant.Id);
            }
            else
            {
                _logger.LogDebug("No tenant found for JWT identifier: {Identifier}", identifier);
            }

            return tenant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving tenant for JWT identifier: {Identifier}", identifier);
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
        var cacheKey = $"tenant:jwt:id:{tenantId}";

        // Try to get from cache first
        if (_cache.TryGetValue(cacheKey, out TenantInfo? cachedTenant))
        {
            _logger.LogDebug("Tenant resolved from cache for JWT ID: {TenantId}", tenantId);
            return cachedTenant;
        }

        try
        {
            var tenant = await _tenantService.GetTenantAsync(tenantId, cancellationToken);
            
            if (tenant != null)
            {
                // Cache the result
                _cache.Set(cacheKey, tenant, CacheDuration);
                _logger.LogDebug("Tenant resolved and cached for JWT ID: {TenantId}", tenantId);
            }
            else
            {
                _logger.LogDebug("No tenant found for JWT ID: {TenantId}", tenantId);
            }

            return tenant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving tenant for JWT ID: {TenantId}", tenantId);
            return null;
        }
    }
}