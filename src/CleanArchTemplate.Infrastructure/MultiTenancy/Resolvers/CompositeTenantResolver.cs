using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Infrastructure.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Infrastructure.MultiTenancy.Resolvers;

/// <summary>
/// Composite tenant resolver that tries multiple resolution strategies in order of priority
/// </summary>
public class CompositeTenantResolver : IHttpTenantResolver
{
    private readonly IEnumerable<IHttpTenantResolver> _resolvers;
    private readonly ILogger<CompositeTenantResolver> _logger;

    public int Priority => 0; // Highest priority (used as the main resolver)
    public string Name => "Composite";

    public CompositeTenantResolver(
        IEnumerable<IHttpTenantResolver> resolvers,
        ILogger<CompositeTenantResolver> logger)
    {
        // Filter out the composite resolver itself to avoid circular dependency
        _resolvers = resolvers.Where(r => r.GetType() != typeof(CompositeTenantResolver))
                             .Cast<IHttpTenantResolver>()
                             .OrderBy(r => r.Priority)
                             .ToList();
        _logger = logger;
    }

    /// <summary>
    /// Resolves tenant information by trying multiple strategies in order of priority
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant information if found</returns>
    public async Task<TenantInfo?> ResolveTenantAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting tenant resolution with {ResolverCount} resolvers", _resolvers.Count());

        foreach (var resolver in _resolvers)
        {
            try
            {
                _logger.LogDebug("Trying tenant resolver: {ResolverName} (Priority: {Priority})", 
                    resolver.Name, resolver.Priority);

                var tenant = await resolver.ResolveTenantAsync(context, cancellationToken);
                
                if (tenant != null)
                {
                    _logger.LogInformation("Tenant resolved successfully using {ResolverName}: {TenantId} ({TenantName})", 
                        resolver.Name, tenant.Id, tenant.Name);
                    return tenant;
                }

                _logger.LogDebug("Resolver {ResolverName} did not find a tenant", resolver.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in tenant resolver {ResolverName}", resolver.Name);
                // Continue to next resolver
            }
        }

        _logger.LogDebug("No tenant found after trying all resolvers");
        return null;
    }

    /// <summary>
    /// Resolves tenant information from an identifier by trying multiple strategies
    /// </summary>
    /// <param name="identifier">The tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant information if found</returns>
    public async Task<TenantInfo?> ResolveTenantAsync(string identifier, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return null;

        _logger.LogDebug("Starting tenant resolution by identifier: {Identifier}", identifier);

        foreach (var resolver in _resolvers)
        {
            try
            {
                _logger.LogDebug("Trying tenant resolver: {ResolverName} for identifier: {Identifier}", 
                    resolver.Name, identifier);

                var tenant = await resolver.ResolveTenantAsync(identifier, cancellationToken);
                
                if (tenant != null)
                {
                    _logger.LogInformation("Tenant resolved successfully using {ResolverName} for identifier {Identifier}: {TenantId} ({TenantName})", 
                        resolver.Name, identifier, tenant.Id, tenant.Name);
                    return tenant;
                }

                _logger.LogDebug("Resolver {ResolverName} did not find tenant for identifier: {Identifier}", 
                    resolver.Name, identifier);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in tenant resolver {ResolverName} for identifier: {Identifier}", 
                    resolver.Name, identifier);
                // Continue to next resolver
            }
        }

        _logger.LogDebug("No tenant found for identifier: {Identifier}", identifier);
        return null;
    }
}