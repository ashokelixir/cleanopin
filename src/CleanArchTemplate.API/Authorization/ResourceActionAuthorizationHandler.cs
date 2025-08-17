using Microsoft.AspNetCore.Authorization;
using CleanArchTemplate.Application.Common.Interfaces;

namespace CleanArchTemplate.API.Authorization;

/// <summary>
/// Authorization handler for resource-action requirements
/// </summary>
public class ResourceActionAuthorizationHandler : AuthorizationHandler<ResourceActionRequirement>
{
    private readonly IPermissionAuthorizationService _permissionAuthorizationService;
    private readonly ILogger<ResourceActionAuthorizationHandler> _logger;

    public ResourceActionAuthorizationHandler(
        IPermissionAuthorizationService permissionAuthorizationService,
        ILogger<ResourceActionAuthorizationHandler> logger)
    {
        _permissionAuthorizationService = permissionAuthorizationService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the authorization requirement
    /// </summary>
    /// <param name="context">The authorization handler context</param>
    /// <param name="requirement">The resource-action requirement</param>
    /// <returns>A task representing the asynchronous operation</returns>
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceActionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("Authorization failed: User is not authenticated for resource {Resource} and action {Action}", 
                requirement.Resource, requirement.Action);
            context.Fail();
            return;
        }

        try
        {
            var authorizationResult = await _permissionAuthorizationService.AuthorizeAsync(
                context.User, 
                requirement.Resource, 
                requirement.Action);

            if (authorizationResult.IsAuthorized)
            {
                _logger.LogDebug("Authorization successful: User has permission for resource {Resource} and action {Action}", 
                    requirement.Resource, requirement.Action);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogInformation("Authorization failed: User does not have permission for resource {Resource} and action {Action}. Reason: {Reason}", 
                    requirement.Resource, requirement.Action, authorizationResult.FailureReason);
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authorization for resource {Resource} and action {Action}", 
                requirement.Resource, requirement.Action);
            context.Fail();
        }
    }
}