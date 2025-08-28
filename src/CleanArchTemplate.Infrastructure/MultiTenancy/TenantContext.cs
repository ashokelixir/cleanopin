using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Models;

namespace CleanArchTemplate.Infrastructure.MultiTenancy;

/// <summary>
/// Service for managing the current tenant context
/// </summary>
public class TenantContext : ITenantContext
{
    private TenantInfo? _currentTenant;

    /// <summary>
    /// Gets the current tenant information
    /// </summary>
    public TenantInfo? CurrentTenant => _currentTenant;

    /// <summary>
    /// Indicates whether the application is running in multi-tenant mode
    /// </summary>
    public bool IsMultiTenant => true; // This template is designed for multi-tenancy

    /// <summary>
    /// Sets the current tenant
    /// </summary>
    /// <param name="tenant">The tenant to set as current</param>
    public void SetTenant(TenantInfo? tenant)
    {
        _currentTenant = tenant;
    }

    /// <summary>
    /// Clears the current tenant
    /// </summary>
    public void ClearTenant()
    {
        _currentTenant = null;
    }

    /// <summary>
    /// Gets the current tenant ID
    /// </summary>
    /// <returns>The current tenant ID, or null if no tenant is set</returns>
    public Guid? GetCurrentTenantId()
    {
        return _currentTenant?.Id;
    }

    /// <summary>
    /// Ensures that a tenant is set and throws an exception if not
    /// </summary>
    /// <returns>The current tenant</returns>
    /// <exception cref="InvalidOperationException">Thrown when no tenant is set</exception>
    public TenantInfo EnsureTenant()
    {
        return _currentTenant ?? throw new InvalidOperationException("No tenant context is available. Ensure that the tenant middleware is properly configured.");
    }
}