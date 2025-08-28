using CleanArchTemplate.Application.Common.Models;

namespace CleanArchTemplate.Application.Common.Interfaces;

/// <summary>
/// Interface for resolving tenant information from various sources
/// </summary>
public interface ITenantResolver
{
    /// <summary>
    /// Resolves tenant information from an identifier
    /// </summary>
    /// <param name="identifier">The tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant information if found</returns>
    Task<TenantInfo?> ResolveTenantAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the priority of this resolver (lower values have higher priority)
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets the name of this resolver for logging purposes
    /// </summary>
    string Name { get; }
}