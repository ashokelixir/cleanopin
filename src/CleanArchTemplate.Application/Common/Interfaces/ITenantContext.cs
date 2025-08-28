using CleanArchTemplate.Application.Common.Models;

namespace CleanArchTemplate.Application.Common.Interfaces;

/// <summary>
/// Interface for managing the current tenant context
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Gets the current tenant information
    /// </summary>
    TenantInfo? CurrentTenant { get; }

    /// <summary>
    /// Indicates whether the application is running in multi-tenant mode
    /// </summary>
    bool IsMultiTenant { get; }

    /// <summary>
    /// Sets the current tenant
    /// </summary>
    /// <param name="tenant">The tenant to set as current</param>
    void SetTenant(TenantInfo? tenant);

    /// <summary>
    /// Clears the current tenant
    /// </summary>
    void ClearTenant();

    /// <summary>
    /// Gets the current tenant ID
    /// </summary>
    /// <returns>The current tenant ID, or null if no tenant is set</returns>
    Guid? GetCurrentTenantId();

    /// <summary>
    /// Ensures that a tenant is set and throws an exception if not
    /// </summary>
    /// <returns>The current tenant</returns>
    /// <exception cref="InvalidOperationException">Thrown when no tenant is set</exception>
    TenantInfo EnsureTenant();
}