using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Models;

namespace CleanArchTemplate.API.Attributes;

/// <summary>
/// Authorization attribute that requires a specific permission
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _permission;

    /// <summary>
    /// Initializes a new instance of the RequirePermissionAttribute
    /// </summary>
    /// <param name="permission">The required permission name</param>
    public RequirePermissionAttribute(string permission)
    {
        _permission = permission ?? throw new ArgumentNullException(nameof(permission));
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
            var authorizationResult = await authorizationService.AuthorizeAsync(user, _permission);
            
            if (!authorizationResult.IsAuthorized)
            {
                var errorResponse = new
                {
                    error = "Insufficient permissions",
                    message = authorizationResult.FailureReason ?? "Access denied",
                    requiredPermission = _permission,
                    userPermissions = authorizationResult.UserPermissions
                };

                context.Result = new ForbidResult();
                context.HttpContext.Response.StatusCode = 403;
                
                // Add error details to response headers for debugging (in development)
                if (context.HttpContext.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true)
                {
                    context.HttpContext.Response.Headers["X-Permission-Error"] = authorizationResult.FailureReason ?? "Access denied";
                    context.HttpContext.Response.Headers["X-Required-Permission"] = _permission;
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