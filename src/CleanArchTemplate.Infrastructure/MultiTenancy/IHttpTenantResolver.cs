using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Models;
using Microsoft.AspNetCore.Http;

namespace CleanArchTemplate.Infrastructure.MultiTenancy;

/// <summary>
/// Interface for resolving tenant information from HTTP context
/// </summary>
public interface IHttpTenantResolver : ITenantResolver
{
    /// <summary>
    /// Resolves tenant information from the HTTP context
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant information if found</returns>
    Task<TenantInfo?> ResolveTenantAsync(HttpContext context, CancellationToken cancellationToken = default);
}