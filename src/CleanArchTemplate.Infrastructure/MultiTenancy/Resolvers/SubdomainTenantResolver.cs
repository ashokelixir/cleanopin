using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Infrastructure.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Infrastructure.MultiTenancy.Resolvers;

/// <summary>
/// Resolves tenant information from subdomain
/// </summary>
public class SubdomainTenantResolver : IHttpTenantResolver
{
    private readonly ITenantService _tenantService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SubdomainTenantResolver> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    public int Priority => 1; // High priority
    public string Name => "Subdomain";

    public SubdomainTenantResolver(
        ITenantService tenantService,
        IMemoryCache cache,
        ILogger<SubdomainTenantResolver> logger)
    {
        _tenantService = tenantService;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Resolves tenant information from the HTTP context subdomain
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant information if found</returns>
    public async Task<TenantInfo?> ResolveTenantAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        var host = context.Request.Host.Host;
        var subdomain = ExtractSubdomain(host);

        if (string.IsNullOrEmpty(subdomain))
        {
            _logger.LogDebug("No subdomain found in host: {Host}", host);
            return null;
        }

        return await ResolveTenantAsync(subdomain, cancellationToken);
    }

    /// <summary>
    /// Resolves tenant information from an identifier (subdomain)
    /// </summary>
    /// <param name="identifier">The tenant identifier (subdomain)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant information if found</returns>
    public async Task<TenantInfo?> ResolveTenantAsync(string identifier, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return null;

        var cacheKey = $"tenant:subdomain:{identifier.ToLowerInvariant()}";

        // Try to get from cache first
        if (_cache.TryGetValue(cacheKey, out TenantInfo? cachedTenant))
        {
            _logger.LogDebug("Tenant resolved from cache for subdomain: {Subdomain}", identifier);
            return cachedTenant;
        }

        try
        {
            var tenant = await _tenantService.GetTenantByIdentifierAsync(identifier, cancellationToken);
            
            if (tenant != null)
            {
                // Cache the result
                _cache.Set(cacheKey, tenant, CacheDuration);
                _logger.LogDebug("Tenant resolved and cached for subdomain: {Subdomain}, TenantId: {TenantId}", 
                    identifier, tenant.Id);
            }
            else
            {
                _logger.LogDebug("No tenant found for subdomain: {Subdomain}", identifier);
            }

            return tenant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving tenant for subdomain: {Subdomain}", identifier);
            return null;
        }
    }

    /// <summary>
    /// Extracts the subdomain from a host name
    /// </summary>
    /// <param name="host">The host name</param>
    /// <returns>The subdomain if found</returns>
    private static string? ExtractSubdomain(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return null;

        // Remove port if present
        var hostWithoutPort = host.Split(':')[0];
        
        // Split by dots
        var parts = hostWithoutPort.Split('.');
        
        // Need at least 3 parts for a subdomain (subdomain.domain.tld)
        if (parts.Length < 3)
            return null;

        // Return the first part as subdomain
        var subdomain = parts[0].ToLowerInvariant();
        
        // Skip common subdomains that are not tenant identifiers
        var reservedSubdomains = new[] { "www", "api", "admin", "app", "mail", "ftp" };
        if (reservedSubdomains.Contains(subdomain))
            return null;

        return subdomain;
    }
}