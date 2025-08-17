using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using CleanArchTemplate.Application.Common.Interfaces;

namespace CleanArchTemplate.API.Attributes;

/// <summary>
/// Authorization attribute that requires a specific resource-action permission
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireResourceActionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _resource;
    private readonly string _action;

    /// <summary>
    /// Initializes a new instance of the RequireResourceActionAttribute
    /// </summary>
    /// <param name="resource">The resource name</param>
    /// <param name="action">The action name</param>
    public RequireResourceActionAttribute(string resource, string action)
    {
        _resource = resource ?? throw new ArgumentNullException(nameof(resource));
        _action = action ?? throw new ArgumentNullException(nameof(action));
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
            var authorizationResult = await authorizationService.AuthorizeAsync(user, _resource, _action);
            
            if (!authorizationResult.IsAuthorized)
            {
                var permissionName = $"{_resource}.{_action}";
                var errorResponse = new
                {
                    error = "Insufficient permissions",
                    message = authorizationResult.FailureReason ?? "Access denied",
                    requiredPermission = permissionName,
                    resource = _resource,
                    action = _action,
                    userPermissions = authorizationResult.UserPermissions
                };

                context.Result = new ForbidResult();
                context.HttpContext.Response.StatusCode = 403;
                
                // Add error details to response headers for debugging (in development)
                if (context.HttpContext.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true)
                {
                    context.HttpContext.Response.Headers["X-Permission-Error"] = authorizationResult.FailureReason ?? "Access denied";
                    context.HttpContext.Response.Headers["X-Required-Permission"] = permissionName;
                    context.HttpContext.Response.Headers["X-Resource"] = _resource;
                    context.HttpContext.Response.Headers["X-Action"] = _action;
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