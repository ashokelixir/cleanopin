using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace CleanArchTemplate.API.Authorization;

/// <summary>
/// Custom authorization policy provider for permission-based policies
/// </summary>
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    /// <summary>
    /// Gets the default authorization policy
    /// </summary>
    /// <returns>The default policy</returns>
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _fallbackPolicyProvider.GetDefaultPolicyAsync();
    }

    /// <summary>
    /// Gets the fallback authorization policy
    /// </summary>
    /// <returns>The fallback policy</returns>
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return _fallbackPolicyProvider.GetFallbackPolicyAsync();
    }

    /// <summary>
    /// Gets an authorization policy by name
    /// </summary>
    /// <param name="policyName">The policy name</param>
    /// <returns>The authorization policy</returns>
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PermissionPolicyConstants.PERMISSION_POLICY_PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            var permission = policyName[PermissionPolicyConstants.PERMISSION_POLICY_PREFIX.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        if (policyName.StartsWith(PermissionPolicyConstants.RESOURCE_ACTION_POLICY_PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            var resourceAction = policyName[PermissionPolicyConstants.RESOURCE_ACTION_POLICY_PREFIX.Length..];
            var parts = resourceAction.Split('.');
            if (parts.Length == 2)
            {
                var policy = new AuthorizationPolicyBuilder()
                    .AddRequirements(new ResourceActionRequirement(parts[0], parts[1]))
                    .Build();
                return Task.FromResult<AuthorizationPolicy?>(policy);
            }
        }

        if (policyName.StartsWith(PermissionPolicyConstants.ANY_PERMISSION_POLICY_PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            var permissionsString = policyName[PermissionPolicyConstants.ANY_PERMISSION_POLICY_PREFIX.Length..];
            var permissions = permissionsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToArray();
            
            if (permissions.Length > 0)
            {
                var policy = new AuthorizationPolicyBuilder()
                    .AddRequirements(new AnyPermissionRequirement(permissions))
                    .Build();
                return Task.FromResult<AuthorizationPolicy?>(policy);
            }
        }

        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }
}

/// <summary>
/// Constants for permission policy names
/// </summary>
public static class PermissionPolicyConstants
{
    public const string PERMISSION_POLICY_PREFIX = "Permission:";
    public const string RESOURCE_ACTION_POLICY_PREFIX = "ResourceAction:";
    public const string ANY_PERMISSION_POLICY_PREFIX = "AnyPermission:";
}