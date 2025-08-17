using Microsoft.AspNetCore.Authorization;
using CleanArchTemplate.API.Authorization;

namespace CleanArchTemplate.API.Extensions;

/// <summary>
/// Extension methods for configuring authorization services
/// </summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Adds permission-based authorization services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddPermissionBasedAuthorization(this IServiceCollection services)
    {
        // Register authorization handlers
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, ResourceActionAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, AnyPermissionAuthorizationHandler>();

        // Register custom policy provider
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        return services;
    }

    /// <summary>
    /// Adds predefined permission policies
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddPermissionPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Add common permission policies
            options.AddPolicy("Users.Create", policy => 
                policy.Requirements.Add(new PermissionRequirement("Users.Create")));
            
            options.AddPolicy("Users.Read", policy => 
                policy.Requirements.Add(new PermissionRequirement("Users.Read")));
            
            options.AddPolicy("Users.Update", policy => 
                policy.Requirements.Add(new PermissionRequirement("Users.Update")));
            
            options.AddPolicy("Users.Delete", policy => 
                policy.Requirements.Add(new PermissionRequirement("Users.Delete")));

            options.AddPolicy("Roles.Create", policy => 
                policy.Requirements.Add(new PermissionRequirement("Roles.Create")));
            
            options.AddPolicy("Roles.Read", policy => 
                policy.Requirements.Add(new PermissionRequirement("Roles.Read")));
            
            options.AddPolicy("Roles.Update", policy => 
                policy.Requirements.Add(new PermissionRequirement("Roles.Update")));
            
            options.AddPolicy("Roles.Delete", policy => 
                policy.Requirements.Add(new PermissionRequirement("Roles.Delete")));

            options.AddPolicy("Permissions.Create", policy => 
                policy.Requirements.Add(new PermissionRequirement("Permissions.Create")));
            
            options.AddPolicy("Permissions.Read", policy => 
                policy.Requirements.Add(new PermissionRequirement("Permissions.Read")));
            
            options.AddPolicy("Permissions.Update", policy => 
                policy.Requirements.Add(new PermissionRequirement("Permissions.Update")));
            
            options.AddPolicy("Permissions.Delete", policy => 
                policy.Requirements.Add(new PermissionRequirement("Permissions.Delete")));

            // Add resource-action policies
            options.AddPolicy("ResourceAction.Users.Create", policy => 
                policy.Requirements.Add(new ResourceActionRequirement("Users", "Create")));
            
            options.AddPolicy("ResourceAction.Users.Read", policy => 
                policy.Requirements.Add(new ResourceActionRequirement("Users", "Read")));

            // Add any permission policies
            options.AddPolicy("AnyUserPermission", policy => 
                policy.Requirements.Add(new AnyPermissionRequirement("Users.Create", "Users.Read", "Users.Update", "Users.Delete")));
            
            options.AddPolicy("AnyRolePermission", policy => 
                policy.Requirements.Add(new AnyPermissionRequirement("Roles.Create", "Roles.Read", "Roles.Update", "Roles.Delete")));

            // Add admin policy that requires any admin permission
            options.AddPolicy("AdminAccess", policy => 
                policy.Requirements.Add(new AnyPermissionRequirement(
                    "Users.Create", "Users.Update", "Users.Delete",
                    "Roles.Create", "Roles.Update", "Roles.Delete",
                    "Permissions.Create", "Permissions.Update", "Permissions.Delete")));
        });

        return services;
    }
}