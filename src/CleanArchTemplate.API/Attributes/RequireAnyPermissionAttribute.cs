using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using CleanArchTemplate.Application.Common.Interfaces;

namespace CleanArchTemplate.API.Attributes;

/// <summary>
/// Authorization attribute that requires any one of the specified permissions
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireAnyPermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _permissions;

    /// <summary>
    /// Initializes a new instance of the RequireAnyPermissionAttribute
    /// </summary>
    /// <param name="permissions">The permissions (user needs any one of these)</param>
    public RequireAnyPermissionAttribute(params string[] permissions)
    {
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        
        if (_permissions.Length == 0)
        {
            throw new ArgumentException("At least one permission must be specified", nameof(permissions));
        }
    }

    /// <summary>
    /// Performs authorization check
    /// </summary>
    /// <param name="context">The authorization filter context</param>
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Skip authorization if the action has AllowAnonymous attribute
        if (context.ActionDescriptor.EndpointMetadata.Any(em => em.GetType() == typeof(AllowAnonymousAttribute)))
        {
            return;
        }

        var user = context.HttpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                error = "Authentication required",
                message = "User must be authenticated to access this resource"
            });
            return;
        }

        var authorizationService = context.HttpContext.RequestServices.GetService<IPermissionAuthorizationService>();
        if (authorizationService == null)
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        try
        {
            var authorizationResult = await authorizationService.AuthorizeAnyAsync(user, _permissions);
            
            if (!authorizationResult.IsAuthorized)
            {
                var permissionsString = string.Join(", ", _permissions);
                var errorResponse = new
                {
                    error = "Insufficient permissions",
                    message = authorizationResult.FailureReason ?? "Access denied",
                    requiredPermissions = _permissions,
                    requirementType = "any",
                    userPermissions = authorizationResult.UserPermissions
                };

                context.Result = new ForbidResult();
                context.HttpContext.Response.StatusCode = 403;
                
                // Add error details to response headers for debugging (in development)
                if (context.HttpContext.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true)
                {
                    context.HttpContext.Response.Headers["X-Permission-Error"] = authorizationResult.FailureReason ?? "Access denied";
                    context.HttpContext.Response.Headers["X-Required-Permissions"] = permissionsString;
                    context.HttpContext.Response.Headers["X-Requirement-Type"] = "any";
                }
            }
        }
        catch (Exception)
        {
            // Log the exception (you might want to inject ILogger here)
            context.Result = new StatusCodeResult(500);
        }
    }
}